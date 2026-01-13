using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NisSystem.API.Models;

/// <summary>
/// 医嘱
/// </summary>
[Table("MedicationOrders")]
public class MedicationOrder : BaseEntity
{
    // 外键
    public Guid PatientId { get; set; }
    
    public string OrderNumber { get; set; } = string.Empty; // 医嘱号
    public string MedicationName { get; set; } = string.Empty; // 医嘱内容
    public string? Specification { get; set; } // 规格/类型
    public decimal? Quantity { get; set; } // 单量
    public string? Unit { get; set; } // 单位
    public string Dosage { get; set; } = string.Empty; // 剂量（保留用于兼容）
    public string Frequency { get; set; } = string.Empty; // 频次
    public string Route { get; set; } = string.Empty; // 用法（给药途径）
    public string OrderType { get; set; } = "长期"; // 长期/临时
    public DateTime OrderDate { get; set; } = DateTime.UtcNow;
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public DateTime? ScheduledTime { get; set; } // 计划执行时间
    public string Status { get; set; } = "待执行"; // 待执行/执行中/已完成/已停止
    public string? DoctorName { get; set; } // 开医嘱医生
    public string? Notes { get; set; } // 备注
    public bool HasContraindication { get; set; } = false; // 是否有配伍禁忌
    public bool HasAllergyWarning { get; set; } = false; // 是否有过敏警告
    
    // 导航属性
    public Patient Patient { get; set; } = null!;
    public ICollection<MedicationExecution> Executions { get; set; } = new List<MedicationExecution>();
}

