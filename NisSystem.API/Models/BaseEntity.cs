using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NisSystem.API.Models;

/// <summary>
/// 基础实体类，包含通用字段
/// </summary>
public abstract class BaseEntity
{
    /// <summary>
    /// 主键
    /// </summary>
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();
    
    /// <summary>
    /// 创建时间
    /// </summary>
    [Column(TypeName = "timestamp with time zone")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// 更新时间
    /// </summary>
    [Column(TypeName = "timestamp with time zone")]
    public DateTime? UpdatedAt { get; set; }
    
    /// <summary>
    /// 创建人
    /// </summary>
    [Column(TypeName = "text")]
    public string? CreatedBy { get; set; }
    
    /// <summary>
    /// 更新人
    /// </summary>
    [Column(TypeName = "text")]
    public string? UpdatedBy { get; set; }
    
    /// <summary>
    /// 是否已删除（软删除）
    /// </summary>
    [Column(TypeName = "boolean")]
    public bool IsDeleted { get; set; } = false;
}

