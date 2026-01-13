using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NisSystem.API.Models;

/// <summary>
/// 审计日志
/// </summary>
[Table("AuditLogs")]
public class AuditLog : BaseEntity
{
    // 外键
    public Guid? UserId { get; set; }
    
    public string Action { get; set; } = string.Empty; // 操作类型：Create/Update/Delete/Login等
    public string EntityType { get; set; } = string.Empty; // 实体类型
    public Guid? EntityId { get; set; } // 实体ID
    public string? OldValues { get; set; } // 旧值（JSON格式）
    public string? NewValues { get; set; } // 新值（JSON格式）
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public string? Description { get; set; } // 操作描述
    
    // 导航属性
    public User? User { get; set; }
}

