namespace NisSystem.API.DTOs;

/// <summary>
/// 更新生命体征记录 DTO
/// </summary>
public class UpdateVitalSignsDto
{
    public DateTime RecordedAt { get; set; }
    public decimal? Temperature { get; set; }
    public int? Pulse { get; set; }
    public int? Respiration { get; set; }
    public int? SystolicBP { get; set; }
    public int? DiastolicBP { get; set; }
    public int? OxygenSaturation { get; set; }
    public decimal? BloodGlucose { get; set; }
    public string? Notes { get; set; }
}

