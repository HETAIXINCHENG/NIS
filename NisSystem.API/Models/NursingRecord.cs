using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NisSystem.API.Models;

/// <summary>
/// 护理措施记录
/// </summary>
[Table("NursingRecords")]
public class NursingRecord : BaseEntity
{
    // 外键
    public Guid PatientId { get; set; }
    public Guid RecordedByUserId { get; set; }
    
    public string RecordType { get; set; } = string.Empty; // 用药执行、输液、伤口护理、导管护理等
    public string Content { get; set; } = string.Empty; // 记录内容
    public DateTime RecordedAt { get; set; } = DateTime.UtcNow;
    public string? Notes { get; set; } // 备注
    
    // 用药相关字段
    public string? MedicationName { get; set; }
    public string? Dosage { get; set; }
    public string? Route { get; set; } // 给药途径
    
    // 输液相关字段
    public decimal? InfusionRate { get; set; } // 滴速
    public decimal? TotalVolume { get; set; } // 总量
    
    // 生命体征（临床护理记录单）
    public decimal? Temperature { get; set; } // 体温 °C
    public int? Pulse { get; set; } // 脉搏 次/分
    public int? Respiration { get; set; } // 呼吸 次/分
    public string? BloodPressure { get; set; } // 血压，格式：收缩压/舒张压，如 "120/80"
    
    // 输入液及食物
    public string? IntakeName { get; set; } // 输入液及食物名称
    public decimal? IntakeVolume { get; set; } // 输入量 ml
    public string? IntakeRoute { get; set; } // 输入途径（静脉、口服等）
    
    // 排出物
    public string? OutputName { get; set; } // 排出物名称（尿、引流液等）
    public decimal? OutputVolume { get; set; } // 排出量 ml
    
    // 病情与措施
    public string? ConditionAndMeasures { get; set; } // 病情与措施（详细记录）
    
    // 导航属性
    public Patient Patient { get; set; } = null!;
    public User RecordedBy { get; set; } = null!;
}

