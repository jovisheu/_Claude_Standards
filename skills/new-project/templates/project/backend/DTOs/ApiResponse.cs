namespace backend.DTOs;

/// <summary>
/// API 統一回傳格式：只有 HTTP 200 為 Status = true，StatusCode 與 HTTP 狀態碼相同；失敗訊息一律放在 Message
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

    public static ApiResponse<T> Success(T? datas, string message = "")
        => new() { Status = true, StatusCode = StatusCodes.Status200OK, Message = message, Datas = datas };

    public static ApiResponse<T> Fail(int statusCode, string message)
        => new() { Status = false, StatusCode = statusCode, Message = message };
}

/// <summary>無資料的回應</summary>
public class ApiResponse : ApiResponse<object>
{
    public static ApiResponse Ok(string message = "")
        => new() { Status = true, StatusCode = StatusCodes.Status200OK, Message = message };

    public static new ApiResponse Fail(int statusCode, string message)
        => new() { Status = false, StatusCode = statusCode, Message = message };

    /// <summary>分頁結果，展開至頂層的 totalCount、page、pageSize、totalPages</summary>
    public static ApiResponse<IReadOnlyList<T>> Paged<T>(PagedResult<T> result)
        => new()
        {
            Status = true,
            StatusCode = StatusCodes.Status200OK,
            Datas = result.Items,
            TotalCount = result.TotalCount,
            Page = result.Page,
            PageSize = result.PageSize,
            TotalPages = result.TotalPages,
        };
}
