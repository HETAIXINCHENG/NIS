using System.ComponentModel.DataAnnotations;

namespace NisSystem.API.DTOs;

/// <summary>
/// 创建用药执行记录DTO
/// </summary>
public class CreateMedicationExecutionDto
{
    [Required(ErrorMessage = "医嘱ID不能为空")]
    public Guid MedicationOrderId { get; set; }

    public string? ExecutedTime { get; set; } // 执行时间（字符串格式）

    public string? Status { get; set; } // 执行状态

    public string? Notes { get; set; } // 备注

    public string? VerificationCode { get; set; } // 核对码
}

