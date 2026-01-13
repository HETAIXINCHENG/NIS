using System.ComponentModel.DataAnnotations;

namespace NisSystem.API.DTOs;

/// <summary>
/// 创建医嘱DTO
/// </summary>
public class CreateMedicationOrderDto
{
    [Required(ErrorMessage = "患者ID不能为空")]
    public Guid PatientId { get; set; }

    public string? OrderNumber { get; set; } // 医嘱号（可选，如果不提供则自动生成）

    [Required(ErrorMessage = "医嘱内容不能为空")]
    public string MedicationName { get; set; } = string.Empty; // 医嘱内容

    public string? Specification { get; set; } // 规格/类型

    public decimal? Quantity { get; set; } // 单量

    public string? Unit { get; set; } // 单位

    [Required(ErrorMessage = "频次不能为空")]
    public string Frequency { get; set; } = string.Empty; // 频次

    [Required(ErrorMessage = "用法不能为空")]
    public string Route { get; set; } = string.Empty; // 用法

    [Required(ErrorMessage = "医嘱类型不能为空")]
    public string OrderType { get; set; } = "长期"; // 长期/临时

    public string? StartDate { get; set; } // 支持字符串格式 "YYYY-MM-DD HH:mm:ss"

    public string? EndDate { get; set; } // 支持字符串格式 "YYYY-MM-DD HH:mm:ss"

    public string? ScheduledTime { get; set; } // 计划执行时间，支持字符串格式 "YYYY-MM-DD HH:mm:ss"

    public string? DoctorName { get; set; } // 开医嘱医生

    public string? Notes { get; set; } // 备注
}

