using System.ComponentModel.DataAnnotations;
using backend.Extensions;

namespace backend.DTOs;

/// <summary>分頁查詢條件</summary>
public class PagedQuery
{
    [Range(1, int.MaxValue, ErrorMessage = "頁數須為正整數")]
    public int Page { get; set; } = 1;

    [Range(1, 100, ErrorMessage = "每頁筆數須介於 1 到 100")]
    public int PageSize { get; set; } = 10;

    [StringLength(50, ErrorMessage = "關鍵字不可超過 50 字")]
    [NoHtml]
    public string? Keyword { get; set; }
}
