using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NisSystem.API.Models;

/// <summary>
/// 专科护理记录
/// </summary>
[Table("SpecialtyCares")]
public class SpecialtyCare : BaseEntity
{
    // 外键
    public Guid PatientId { get; set; }
    public Guid RecordedByUserId { get; set; }
    
    public string SpecialtyType { get; set; } = string.Empty; // ICU/内科/外科
    public string CareType { get; set; } = string.Empty; // 护理类型
    
    // ICU相关
    public decimal? VentilatorPressure { get; set; } // 呼吸机压力
    public int? VentilatorRate { get; set; } // 呼吸频率
    public decimal? CardiacOutput { get; set; } // 心输出量
    public decimal? BloodPressure { get; set; } // 血压
    
    // 内科相关
    public decimal? BloodGlucoseTrend { get; set; } // 血糖趋势
    public decimal? BloodPressureTrend { get; set; } // 血压趋势
    
    // 外科相关
    public string? WoundHealingStatus { get; set; } // 伤口愈合情况
    public string? DrainageStatus { get; set; } // 引流情况
    public decimal? DrainageVolume { get; set; } // 引流量
    
    public string? Details { get; set; } // 详细信息（JSON格式）
    public DateTime RecordedAt { get; set; } = DateTime.UtcNow;
    
    // 导航属性
    public Patient Patient { get; set; } = null!;
    public User RecordedBy { get; set; } = null!;
}

