namespace NisSystem.API.DTOs;

/// <summary>
/// 更新用药执行记录DTO
/// </summary>
public class UpdateMedicationExecutionDto
{
    public string? Status { get; set; }

    public string? ExecutedTime { get; set; }

    public string? Notes { get; set; }
}

