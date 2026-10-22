using backend.Data;
using backend.Extensions;
using backend.Services.Auth;
using backend.Services.BasicChecklogintimeSendMails;
using backend.Services.BasicExceptionlogs;
using backend.Services.BasicGroups;
using backend.Services.BasicPermissions;
using backend.Services.BasicSyslogs;
using backend.Services.BasicUsers;
using backend.Services.Captcha;
using backend.Services.Mail;
using backend.Services.Password;
using backend.Services.Token;
using backend.Settings;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

// 工具模式：dotnet run -- encrypt "<明文>"，以輸出目錄 appsettings.json 的 AES256Key／AES256IV 產生加密值
if (args.Length >= 1 && args[0] == "encrypt")
{
    if (args.Length < 2 || string.IsNullOrEmpty(args[1]))
    {
        Console.Error.WriteLine("用法：dotnet run -- encrypt \"<明文>\"");
        return 1;
    }

    var toolConfiguration = new ConfigurationBuilder()
        .SetBasePath(AppContext.BaseDirectory)
        .AddJsonFile("appsettings.json", optional: false)
        .Build();
    var (toolKey, toolIv) = ConfigEncryption.ReadKey(toolConfiguration);
    Console.WriteLine(ConfigEncryption.Encrypt(args[1], toolKey, toolIv));
    return 0;
}

var builder = WebApplication.CreateBuilder(args);
var configuration = builder.Configuration;

// 機密設定於註冊服務前解密，只存於區域變數（不寫回 IConfiguration，避免設定檔重新載入時還原成加密值）
var (aesKey, aesIv) = ConfigEncryption.ReadKey(configuration);
string DecryptSetting(string name)
{
    var cipherText = configuration[name];
    if (string.IsNullOrEmpty(cipherText))
    {
        throw new InvalidOperationException($"缺少設定值 {name}");
    }

    return ConfigEncryption.Decrypt(cipherText, aesKey, aesIv, name);
}

var connectionString = DecryptSetting("ConnectionStrings:DefaultConnection");
var jwtKey = DecryptSetting("Jwt:Key");
var smtpPassword = string.IsNullOrEmpty(configuration["Smtp:Password"]) ? string.Empty : DecryptSetting("Smtp:Password");

var jwtSettings = configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>() ?? new JwtSettings();
var securitySettings = configuration.GetSection(SecuritySettings.SectionName).Get<SecuritySettings>() ?? new SecuritySettings();
if (securitySettings.Password.ChecklogintimeSendMailDay < 0 || securitySettings.Password.ChecklogintimeSendMailDay >= securitySettings.Password.ExpireDays)
{
    throw new InvalidOperationException("Security:Password:ChecklogintimeSendMailDay 須介於 0（不寄送）與 ExpireDays 之間");
}

// 不回傳 Server 標頭；限制請求大小
const long maxRequestBodySize = 1024 * 1024;
builder.WebHost.ConfigureKestrel(options =>
{
    options.AddServerHeader = false;
    options.Limits.MaxRequestBodySize = maxRequestBodySize;
});
builder.Services.Configure<IISServerOptions>(options => options.MaxRequestBodySize = maxRequestBodySize);

// 設定
builder.Services.Configure<AppSettings>(configuration.GetSection(AppSettings.SectionName));
builder.Services.Configure<JwtSettings>(configuration.GetSection(JwtSettings.SectionName));
builder.Services.PostConfigure<JwtSettings>(options => options.Key = jwtKey);
builder.Services.Configure<SecuritySettings>(configuration.GetSection(SecuritySettings.SectionName));
builder.Services.Configure<SmtpSettings>(configuration.GetSection(SmtpSettings.SectionName));
builder.Services.PostConfigure<SmtpSettings>(options => options.Password = smtpPassword);
builder.Services.Configure<BasicExceptionlogSettings>(configuration.GetSection(BasicExceptionlogSettings.SectionName));
builder.Services.Configure<BasicSyslogSettings>(configuration.GetSection(BasicSyslogSettings.SectionName));

// 資料庫：SQL Server 2019 基準（相容性層級 150）；異動歷程攔截器與把關以 Scoped 註冊並掛上
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<BasicSyslogSaveTracker>();
builder.Services.AddScoped<BasicSyslogContext>();
builder.Services.AddScoped<BasicSyslogInterceptor>();
builder.Services.AddScoped<BasicSyslogCommandGuard>();
builder.Services.AddDbContext<AppDbContext>((sp, options) => options
    .UseSqlServer(connectionString, sql => sql.UseCompatibilityLevel(150))
    .AddInterceptors(sp.GetRequiredService<BasicSyslogInterceptor>(), sp.GetRequiredService<BasicSyslogCommandGuard>()));

// 服務
builder.Services.AddMemoryCache();
builder.Services.AddSingleton<IBasicExceptionlogsService, BasicExceptionlogsService>();
builder.Services.AddSingleton<IBasicSyslogsService, BasicSyslogsService>();
builder.Services.AddScoped<IBasicSyslogHistoryService, BasicSyslogHistoryService>();
builder.Services.AddScoped<IBasicPermissionsService, BasicPermissionsService>();
builder.Services.AddScoped<IPasswordService, PasswordService>();
builder.Services.AddSingleton<ITokenService, TokenService>();
builder.Services.AddSingleton<ICaptchaService, CaptchaService>();
builder.Services.AddSingleton<MailTemplateStore>();
builder.Services.AddSingleton<MailQueue>();
builder.Services.AddSingleton<IMailService, MailService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IBasicUsersService, BasicUsersService>();
builder.Services.AddScoped<IBasicGroupsService, BasicGroupsService>();
builder.Services.AddHostedService<BasicExceptionlogArchiveService>();
builder.Services.AddHostedService<MailBackgroundService>();
builder.Services.AddHostedService<BasicChecklogintimeSendMailService>();

// Controller：時間格式、模型驗證失敗與無 Body 的錯誤一律回 ApiResponse
builder.Services.AddControllers(options =>
    {
        // 不使用隱含的 Required（訊息為英文），一律於 DTO 明確標註
        options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true;

        // 查詢字串、路由等繫結錯誤改為中文通用訊息，不回傳欄位名稱與輸入值
        var messages = options.ModelBindingMessageProvider;
        messages.SetAttemptedValueIsInvalidAccessor((_, _) => "欄位格式不正確");
        messages.SetMissingBindRequiredValueAccessor(_ => "缺少必要欄位");
        messages.SetMissingKeyOrValueAccessor(() => "缺少必要欄位");
        messages.SetMissingRequestBodyRequiredValueAccessor(() => "請求資料格式不正確");
        messages.SetNonPropertyAttemptedValueIsInvalidAccessor(_ => "欄位格式不正確");
        messages.SetNonPropertyUnknownValueIsInvalidAccessor(() => "欄位格式不正確");
        messages.SetNonPropertyValueMustBeANumberAccessor(() => "欄位須為數字");
        messages.SetUnknownValueIsInvalidAccessor(_ => "欄位格式不正確");
        messages.SetValueIsInvalidAccessor(_ => "欄位格式不正確");
        messages.SetValueMustBeANumberAccessor(_ => "欄位須為數字");
        messages.SetValueMustNotBeNullAccessor(_ => "欄位不可為空白");
    })
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new LocalDateTimeJsonConverter()))
    .ConfigureApiBehaviorOptions(options =>
    {
        options.SuppressMapClientErrors = true;
        options.InvalidModelStateResponseFactory = ModelValidationResponse.Create;
    });
builder.Services.ConfigureHttpJsonOptions(options => options.SerializerOptions.Converters.Add(new LocalDateTimeJsonConverter()));

// JWT：驗證簽章、Issuer、Audience 與有效期限；未登入回 401 ApiResponse
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtSettings.Audience,
            ValidateLifetime = true,
            RequireExpirationTime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = TokenService.CreateSigningKey(jwtKey),
            ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
            ClockSkew = TimeSpan.FromSeconds(30),
            NameClaimType = JwtRegisteredClaimNames.UniqueName,
        };
        options.Events = new JwtBearerEvents
        {
            // 停用、已刪除或密碼已變更（安全戳記不符）的使用者，即使 Token 未過期也視為未登入（回 401，前端導回登入頁）
            OnTokenValidated = async context =>
            {
                var basicUserId = context.Principal?.FindBasicUserId();
                var db = context.HttpContext.RequestServices.GetRequiredService<AppDbContext>();
                var passwordHash = basicUserId is null
                    ? null
                    : await db.BasicUser.AsNoTracking()
                        .Where(u => u.Id == basicUserId && u.IsActive)
                        .Select(u => u.PasswordHash)
                        .FirstOrDefaultAsync();
                var securityStamp = context.Principal?.FindFirst(TokenService.SecurityStampClaim)?.Value;
                if (passwordHash is null || !TokenService.IsSecurityStampValid(passwordHash, securityStamp))
                {
                    context.Fail("使用者不存在、已停用或密碼已變更");
                }
            },
            OnChallenge = async context =>
            {
                context.HandleResponse();
                await ApiResponseWriter.WriteAsync(context.HttpContext, StatusCodes.Status401Unauthorized, ApiResponseWriter.DefaultMessage(StatusCodes.Status401Unauthorized));
            },
        };
    });

// 授權：所有端點預設須登入（Fallback Policy），權限查資料庫；無權限回 403 ApiResponse
builder.Services.AddBasicPermissionAuthorization();
builder.Services.AddSingleton<IAuthorizationMiddlewareResultHandler, ApiAuthorizationResultHandler>();

builder.Services.AddApiRateLimiter(securitySettings.RateLimit);

// Swagger 僅限 Development
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();
}

var app = builder.Build();

// 郵件範本於啟動時載入，檔案遺失即無法啟動
app.Services.GetRequiredService<MailTemplateStore>();

app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseMiddleware<SecurityHeadersMiddleware>();

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
    app.UseHttpsRedirection();
}

// /api 下沒有 Body 的錯誤（405、415 等）轉為 ApiResponse
app.UseStatusCodePages(async context =>
{
    var httpContext = context.HttpContext;
    if (ApiResponseWriter.IsApiRequest(httpContext))
    {
        var statusCode = httpContext.Response.StatusCode;
        await ApiResponseWriter.WriteAsync(httpContext, statusCode, ApiResponseWriter.DefaultMessage(statusCode));
    }
});

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// SPA 的 index.html 不快取（assets 檔名含 hash，可正常快取）
var spaFileOptions = new StaticFileOptions
{
    OnPrepareResponse = context =>
    {
        if (string.Equals(context.File.Name, "index.html", StringComparison.OrdinalIgnoreCase))
        {
            context.Context.Response.Headers.CacheControl = "no-cache";
        }
    },
};
app.UseStaticFiles(spaFileOptions);

app.UseRouting();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

RequestDelegate notFound = context => ApiResponseWriter.WriteAsync(context, StatusCodes.Status404NotFound, ApiResponseWriter.DefaultMessage(StatusCodes.Status404NotFound));

// 不存在的 API 回 404（不落入 SPA fallback）
app.Map("/api/{**path}", notFound).AllowAnonymous();

// 正式環境 /swagger 明確回 404
if (!app.Environment.IsDevelopment())
{
    app.Map("/swagger/{**path}", notFound).AllowAnonymous();
}

// 不存在的檔案路徑回 404（不被預設授權擋成 401）；其餘路徑交給 SPA
app.MapFallback("{**path:file}", notFound).AllowAnonymous();
app.MapFallbackToFile("index.html", spaFileOptions).AllowAnonymous();

await app.RunAsync();
return 0;
