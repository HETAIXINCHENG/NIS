namespace NisSystem.API.DTOs;

/// <summary>
/// 核对用药执行记录DTO
/// </summary>
public class VerifyMedicationExecutionDto
{
    public string? VerificationCode { get; set; } // 核对码（患者二维码）
}

