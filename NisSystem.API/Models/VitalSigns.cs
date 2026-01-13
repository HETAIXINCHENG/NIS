using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NisSystem.API.Models;

/// <summary>
/// 生命体征记录
/// </summary>
[Table("VitalSigns")]
public class VitalSigns : BaseEntity
{
    // 外键
    public Guid PatientId { get; set; }
    public Guid RecordedByUserId { get; set; }
    
    // 生命体征数据
    public decimal? Temperature { get; set; } // 体温（℃）
    public int? Pulse { get; set; } // 脉搏（次/分）
    public int? Respiration { get; set; } // 呼吸（次/分）
    public int? SystolicBP { get; set; } // 收缩压（mmHg）
    public int? DiastolicBP { get; set; } // 舒张压（mmHg）
    public int? OxygenSaturation { get; set; } // 血氧饱和度（%）
    public decimal? BloodGlucose { get; set; } // 血糖（mmol/L）
    public DateTime RecordedAt { get; set; } = DateTime.UtcNow;
    public string? Notes { get; set; } // 备注
    public bool IsAbnormal { get; set; } = false; // 是否异常
    
    // 导航属性
    public Patient Patient { get; set; } = null!;
    public User RecordedBy { get; set; } = null!;
}

