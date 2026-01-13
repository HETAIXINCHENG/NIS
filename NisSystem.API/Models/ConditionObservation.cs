using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NisSystem.API.Models;

/// <summary>
/// 病情观察记录
/// </summary>
[Table("ConditionObservations")]
public class ConditionObservation : BaseEntity
{
    // 外键
    public Guid PatientId { get; set; }
    public Guid ObservedByUserId { get; set; }
    
    public string ObservationType { get; set; } = string.Empty; // 专科护理、症状记录
    public string Category { get; set; } = string.Empty; // ICU、内科、外科等
    public string Content { get; set; } = string.Empty; // 观察内容
    
    // ICU专科护理
    public string? VentilatorParameters { get; set; } // 呼吸机参数（JSON格式）
    public string? Hemodynamics { get; set; } // 血流动力学（JSON格式）
    
    // 内科专科护理
    public decimal? BloodGlucoseTrend { get; set; } // 血糖趋势
    public string? BloodPressureTrend { get; set; } // 血压趋势（JSON格式）
    
    // 外科专科护理
    public string? WoundHealing { get; set; } // 伤口愈合情况
    public string? Drainage { get; set; } // 引流情况
    
    // 症状记录
    public bool HasNausea { get; set; } = false; // 恶心
    public bool HasVomiting { get; set; } = false; // 呕吐
    public int? PainLevel { get; set; } // 疼痛等级 0-10
    public string? ConsciousnessState { get; set; } // 意识状态
    public string? SleepQuality { get; set; } // 睡眠质量
    
    public DateTime ObservedAt { get; set; } = DateTime.UtcNow;
    public string? Notes { get; set; } // 备注
    
    // 导航属性
    public Patient Patient { get; set; } = null!;
    public User ObservedBy { get; set; } = null!;
}

