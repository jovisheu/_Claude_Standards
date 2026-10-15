using System.Threading.RateLimiting;
using backend.Data;
using backend.Extensions;
using backend.Services.Auth;
using backend.Services.Captcha;
using backend.Services.BasicExceptionlogs;
using backend.Services.BasicGroups;
using backend.Services.Mail;
using backend.Services.Password;
using backend.Services.BasicPermissions;
using backend.Services.BasicSyslogs;
using backend.Services.Token;
using backend.Services.BasicUsers;
using backend.Settings;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;

// 工具模式：dotnet run -- encrypt "<明文>"，以輸出目錄 appsettings.json 的金鑰產生加密值
if (args.Length >= 1 && args[0] == "encrypt")
{
    if (args.Length < 2)
    {
        Console.Error.WriteLine("用法：dotnet run -- encrypt \"<明文>\"");
        return 1;
    }

    var toolConfig = new ConfigurationBuilder()
        .SetBasePath(AppContext.BaseDirectory)
        .AddJsonFile("appsettings.json", optional: false)
        .Build();
    Console.WriteLine(ConfigEncryption.Encrypt(toolConfig, args[1]));
    return 0;
}

var builder = WebApplication.CreateBuilder(args);

// 不回傳 Server 標頭
builder.WebHost.ConfigureKestrel(options => options.AddServerHeader = false);

// 機密於註冊服務前解密，只存於區域變數，不寫回 IConfiguration（避免重新載入時還原成加密值）
var connectionString = ConfigEncryption.DecryptRequired(builder.Configuration, "ConnectionStrings:DefaultConnection");
var jwtKey = ConfigEncryption.DecryptRequired(builder.Configuration, "Jwt:Key");
var smtpPassword = ConfigEncryption.DecryptOptional(builder.Configuration, "Smtp:Password");

builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection(JwtSettings.SectionName));
builder.Services.PostConfigure<JwtSettings>(s => s.Key = jwtKey);
builder.Services.Configure<SecuritySettings>(builder.Configuration.GetSection(SecuritySettings.SectionName));
builder.Services.Configure<AppSettings>(builder.Configuration.GetSection(AppSettings.SectionName));
builder.Services.Configure<SmtpSettings>(builder.Configuration.GetSection(SmtpSettings.SectionName));
builder.Services.Configure<BasicExceptionlogSettings>(builder.Configuration.GetSection(BasicExceptionlogSettings.SectionName));
builder.Services.Configure<BasicSyslogSettings>(builder.Configuration.GetSection(BasicSyslogSettings.SectionName));
builder.Services.PostConfigure<SmtpSettings>(s => s.Password = smtpPassword);

var jwtSettings = builder.Configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>() ?? new JwtSettings();
var securitySettings = builder.Configuration.GetSection(SecuritySettings.SectionName).Get<SecuritySettings>() ?? new SecuritySettings();

// 系統異動歷程：每個 DbContext 掛上一個攔截器，自動記錄新增／修改／刪除
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<BasicSyslogInterceptor>();
builder.Services.AddDbContext<AppDbContext>((sp, options) => options
    // 以 SQL Server 2019（相容性層級 150）為基準，避免 EF Core 產生 2019 不支援的語法
    .UseSqlServer(connectionString, o => o.UseCompatibilityLevel(150))
    .AddInterceptors(sp.GetRequiredService<BasicSyslogInterceptor>()));
builder.Services.AddMemoryCache();

// 服務
builder.Services.AddScoped<IBasicPermissionsService, BasicPermissionsService>();
builder.Services.AddScoped<IPasswordService, PasswordService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IBasicUsersService, BasicUsersService>();
builder.Services.AddScoped<IBasicGroupsService, BasicGroupsService>();
builder.Services.AddScoped<IBasicSyslogHistoryService, BasicSyslogHistoryService>();
builder.Services.AddSingleton<ICaptchaService, CaptchaService>();
builder.Services.AddSingleton<ITokenService, TokenService>();
builder.Services.AddSingleton<MailTemplateStore>();
builder.Services.AddSingleton<MailQueue>();
builder.Services.AddSingleton<IMailService, MailService>();
builder.Services.AddHostedService<MailBackgroundService>();
builder.Services.AddSingleton<IBasicExceptionlogsService, BasicExceptionlogsService>();
builder.Services.AddHostedService<BasicExceptionlogArchiveService>();
builder.Services.AddSingleton<IBasicSyslogsService, BasicSyslogsService>();

// Controller 與模型驗證
builder.Services.AddControllers(options =>
{
    // 不使用隱含的 Required（訊息為英文），一律於 DTO 明確標註
    options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true;
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
// 回傳時間不含毫秒與時區
.AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new LocalDateTimeJsonConverter()));
builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = ModelValidationResponse.Create;
    // 框架錯誤（405、415 等）不轉成 ProblemDetails，交由 StatusCodePages 統一回傳 ApiResponse
    options.SuppressMapClientErrors = true;
});

// JWT 驗證：簽章、Issuer、Audience、期限
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = TokenService.CreateSigningKey(jwtKey),
            ValidateIssuer = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtSettings.Audience,
            ValidateLifetime = true,
            RequireExpirationTime = true,
            ClockSkew = TimeSpan.FromSeconds(30),
            ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
            NameClaimType = JwtRegisteredClaimNames.UniqueName
        };
        options.Events = new JwtBearerEvents
        {
            // 停用或已刪除的使用者，即使 Token 未過期也視為未登入
            OnTokenValidated = async context =>
            {
                var sub = context.Principal?.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
                var db = context.HttpContext.RequestServices.GetRequiredService<AppDbContext>();
                if (!int.TryParse(sub, out var basicUserId) || !await db.BasicUser.AnyAsync(u => u.Id == basicUserId && u.IsActive))
                {
                    context.Fail("使用者不存在或已停用");
                }
            },
            OnChallenge = async context =>
            {
                context.HandleResponse();
                await context.Response.WriteApiResponseAsync(StatusCodes.Status401Unauthorized, "尚未登入或登入已逾時，請重新登入");
            }
        };
    });

// 預設須登入；權限 Policy 名稱與權限名稱相同，一律查資料庫判斷
builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build();
    foreach (var basicPermission in BasicPermissionNames.All)
    {
        options.AddPolicy(basicPermission, policy => policy.RequireAuthenticatedUser().AddRequirements(new BasicPermissionRequirement(basicPermission)));
    }
});
builder.Services.AddScoped<IAuthorizationHandler, BasicPermissionAuthorizationHandler>();
builder.Services.AddSingleton<Microsoft.AspNetCore.Authorization.IAuthorizationMiddlewareResultHandler, ApiAuthorizationResultHandler>();

// 速率限制（每 IP）
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = async (context, cancellationToken) =>
    {
        await context.HttpContext.Response.WriteApiResponseAsync(StatusCodes.Status429TooManyRequests, "請求過於頻繁，請稍後再試", cancellationToken);
    };
    options.AddPolicy(RateLimitPolicies.Login, httpContext => RateLimitPartition.GetFixedWindowLimiter(
        httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = securitySettings.Login.RateLimitPerMinute,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0
        }));
    options.AddPolicy(RateLimitPolicies.Captcha, httpContext => RateLimitPartition.GetFixedWindowLimiter(
        httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = securitySettings.Login.CaptchaRateLimitPerMinute,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0
        }));
});

// Swagger 僅限 Development 註冊
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new OpenApiInfo { Title = "__PROJECT_NAME__ API", Version = "v1" });
        c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            Description = "輸入登入取得的 JWT"
        });
        c.AddSecurityRequirement(document => new OpenApiSecurityRequirement
        {
            [new OpenApiSecuritySchemeReference("Bearer", document)] = []
        });
    });
}

var app = builder.Build();

// 啟動時載入郵件範本，檔案遺失即無法啟動
app.Services.GetRequiredService<MailTemplateStore>();

app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseMiddleware<SecurityHeadersMiddleware>();

// API 的框架錯誤（405、415 等）沒有 Body 時，統一回傳 ApiResponse
app.UseStatusCodePages(async context =>
{
    var response = context.HttpContext.Response;
    if (context.HttpContext.Request.Path.StartsWithSegments("/api"))
    {
        var message = response.StatusCode switch
        {
            StatusCodes.Status404NotFound => "找不到指定的資源",
            StatusCodes.Status405MethodNotAllowed => "不支援的請求方法",
            StatusCodes.Status415UnsupportedMediaType => "請求資料格式不正確",
            >= 500 => "系統發生異常，請聯繫管理員",
            _ => "請求失敗"
        };
        await response.WriteApiResponseAsync(response.StatusCode, message);
    }
});

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseHsts();
    app.UseHttpsRedirection();
}

// SPA 靜態檔：index.html 不快取（assets 檔名含 hash，可正常快取）
var spaFileOptions = new StaticFileOptions
{
    OnPrepareResponse = ctx =>
    {
        if (string.Equals(ctx.File.Name, "index.html", StringComparison.OrdinalIgnoreCase))
        {
            ctx.Context.Response.Headers.CacheControl = "no-cache";
        }
    }
};
app.UseStaticFiles(spaFileOptions);

app.UseRouting();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// 不存在的 API 回 404，不落入 SPA fallback
app.Map("/api/{**path}", (HttpContext context) =>
        context.Response.WriteApiResponseAsync(StatusCodes.Status404NotFound, "找不到指定的資源"))
    .AllowAnonymous();

// 非 Development 時 Swagger 路徑一律 404，不落入 SPA fallback
if (!app.Environment.IsDevelopment())
{
    app.Map("/swagger/{**path}", () => Results.NotFound()).AllowAnonymous();
}

app.MapFallbackToFile("index.html", spaFileOptions).AllowAnonymous();

// 不存在的檔案路徑（含副檔名）回 404，避免被預設授權 Policy 擋成 401
app.MapFallback("{**path:file}", () => Results.NotFound()).AllowAnonymous();

app.Run();
return 0;
