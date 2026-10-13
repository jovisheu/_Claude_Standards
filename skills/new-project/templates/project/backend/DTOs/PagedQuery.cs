using System.ComponentModel.DataAnnotations;
using backend.Extensions;

namespace backend.DTOs;

/// <summary>
/// 分頁查詢條件
/// </summary>
public class PagedQuery
{
    [MaxLength(50, ErrorMessage = "關鍵字最多 50 個字")]
    [NoHtml]
    public string? Keyword { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "頁數須大於 0")]
    public int Page { get; set; } = 1;

    [Range(1, 100, ErrorMessage = "每頁筆數須介於 1～100")]
    public int PageSize { get; set; } = 10;
}
