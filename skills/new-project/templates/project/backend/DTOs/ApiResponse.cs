namespace backend.DTOs;

/// <summary>
/// API 統一回傳格式；只有 HTTP 200 回傳 status: true
/// </summary>
public class ApiResponse<T>
{
    public bool Status { get; set; }
    public int StatusCode { get; set; }
    public string Message { get; set; } = string.Empty;
    public T? Datas { get; set; }
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }

    public static ApiResponse<T> Ok(T? datas, string message = "")
        => new() { Status = true, StatusCode = StatusCodes.Status200OK, Message = message, Datas = datas };
}

/// <summary>
/// 無資料的回應
/// </summary>
public class ApiResponse : ApiResponse<object>
{
    public static ApiResponse Success(string message = "")
        => new() { Status = true, StatusCode = StatusCodes.Status200OK, Message = message };

    public static ApiResponse Fail(int statusCode, string message)
        => new() { Status = false, StatusCode = statusCode, Message = message };

    /// <summary>分頁結果展開至頂層</summary>
    public static ApiResponse<List<T>> Paged<T>(PagedResult<T> result, string message = "")
        => new()
        {
            Status = true,
            StatusCode = StatusCodes.Status200OK,
            Message = message,
            Datas = result.Items,
            TotalCount = result.TotalCount,
            Page = result.Page,
            PageSize = result.PageSize,
            TotalPages = result.TotalPages
        };
}
