using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NisSystem.API.Models;

/// <summary>
/// 系统集成记录
/// </summary>
[Table("SystemIntegrations")]
public class SystemIntegration : BaseEntity
{
    public string SystemName { get; set; } = string.Empty; // HIS、LIS、PACS、药房系统、设备系统
    public string IntegrationType { get; set; } = string.Empty; // 患者信息、医嘱信息、检验结果、影像报告、药物信息、配药确认、生命体征数据
    public string RequestData { get; set; } = string.Empty; // 请求数据（JSON格式）
    public string? ResponseData { get; set; } // 响应数据（JSON格式）
    public string Status { get; set; } = "成功"; // 成功、失败、待处理
    public DateTime IntegratedAt { get; set; } = DateTime.UtcNow;
    public string? ErrorMessage { get; set; } // 错误信息
    public Guid? RelatedPatientId { get; set; } // 关联患者ID
    public Guid? RelatedOrderId { get; set; } // 关联医嘱ID
}
