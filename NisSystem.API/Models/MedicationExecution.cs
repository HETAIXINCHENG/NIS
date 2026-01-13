using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NisSystem.API.Models;

/// <summary>
/// 用药执行记录
/// </summary>
[Table("MedicationExecutions")]
public class MedicationExecution : BaseEntity
{
    // 外键
    public Guid MedicationOrderId { get; set; }
    public Guid ExecutedByUserId { get; set; }
    public Guid? VerifiedByUserId { get; set; } // 核对护士ID
    
    public DateTime ScheduledTime { get; set; } // 计划执行时间
    public DateTime? ExecutedTime { get; set; } // 实际执行时间
    public DateTime? VerifiedTime { get; set; } // 核对时间
    public string Status { get; set; } = "待执行"; // 待执行/已执行/已跳过/异常
    public string? Notes { get; set; } // 异常情况备注
    public bool IsVerified { get; set; } = false; // 是否已核对（扫码）
    public string? VerificationCode { get; set; } // 核对码（患者二维码）
    public string? ElectronicSignature { get; set; } // 电子签名
    
    // 导航属性
    public MedicationOrder MedicationOrder { get; set; } = null!;
    public User ExecutedBy { get; set; } = null!;
    public User? VerifiedBy { get; set; } // 核对护士
}

