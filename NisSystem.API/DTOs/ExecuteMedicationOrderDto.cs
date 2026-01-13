namespace NisSystem.API.DTOs;

/// <summary>
/// 执行医嘱DTO
/// </summary>
public class ExecuteMedicationOrderDto
{
    public string? VerificationCode { get; set; } // 核对码（患者二维码）
    public string? Notes { get; set; } // 异常情况备注
}

