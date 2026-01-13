using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NisSystem.API.Models;

/// <summary>
/// 知识库
/// </summary>
[Table("KnowledgeBases")]
public class KnowledgeBase : BaseEntity
{
    public string Title { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty; // 护理操作规范、专科护理指南、应急处理流程、药物使用手册
    public string SubCategory { get; set; } = string.Empty; // 子分类
    public string Content { get; set; } = string.Empty; // 内容（操作流程等）
    public string? Department { get; set; } // 适用科室（null表示通用）
    public string? VideoUrl { get; set; } // 视频演示链接（单个）
    public string? KeyPoints { get; set; } // 操作要点（JSON格式）
    public string? OperationFlow { get; set; } // 操作流程
    public string? ImageUrls { get; set; } // 图片URLs（JSON数组格式）
    public string? VideoUrls { get; set; } // 视频URLs（JSON数组格式）
    public string? FileUrls { get; set; } // 文件URLs（JSON数组格式，包含PDF、WORD等）
    public string? Description { get; set; } // 描述
    public string? Tags { get; set; } // 标签（逗号分隔）
    public int ViewCount { get; set; } = 0; // 查看次数
    public bool IsActive { get; set; } = true; // 是否启用
}
