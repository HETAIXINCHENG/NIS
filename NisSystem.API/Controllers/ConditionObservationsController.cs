using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NisSystem.API.Data;
using NisSystem.API.Models;
using System.Security.Claims;
using System.Text.Json;

namespace NisSystem.API.Controllers;

/// <summary>
/// 病情观察控制器
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ConditionObservationsController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ConditionObservationsController> _logger;

    public ConditionObservationsController(ApplicationDbContext context, ILogger<ConditionObservationsController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// 获取患者病情观察记录
    /// </summary>
    [HttpGet("patient/{patientId}")]
    [ProducesResponseType(typeof(List<ConditionObservation>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetObservationsByPatient(
        Guid patientId,
        [FromQuery] string? observationType,
        [FromQuery] string? category,
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate)
    {
        var query = _context.ConditionObservations
            .Include(o => o.ObservedBy)
            .Where(o => o.PatientId == patientId)
            .AsQueryable();

        if (!string.IsNullOrEmpty(observationType))
        {
            query = query.Where(o => o.ObservationType == observationType);
        }

        if (!string.IsNullOrEmpty(category))
        {
            query = query.Where(o => o.Category == category);
        }

        if (startDate.HasValue)
        {
            query = query.Where(o => o.ObservedAt >= startDate.Value);
        }

        if (endDate.HasValue)
        {
            query = query.Where(o => o.ObservedAt <= endDate.Value);
        }

        var observations = await query
            .OrderByDescending(o => o.ObservedAt)
            .ToListAsync();

        return Ok(observations);
    }

    /// <summary>
    /// 创建ICU专科护理记录
    /// </summary>
    [HttpPost("icu")]
    [Authorize(Roles = "Admin,Nurse,HeadNurse")]
    [ProducesResponseType(typeof(ConditionObservation), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateIcuObservation([FromBody] IcuObservationDto dto)
    {
        return await CreateObservation(dto, "专科护理", "ICU");
    }

    /// <summary>
    /// 创建内科专科护理记录
    /// </summary>
    [HttpPost("internal-medicine")]
    [Authorize(Roles = "Admin,Nurse,HeadNurse")]
    [ProducesResponseType(typeof(ConditionObservation), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateInternalMedicineObservation([FromBody] InternalMedicineObservationDto dto)
    {
        return await CreateObservation(dto, "专科护理", "内科");
    }

    /// <summary>
    /// 创建外科专科护理记录
    /// </summary>
    [HttpPost("surgery")]
    [Authorize(Roles = "Admin,Nurse,HeadNurse")]
    [ProducesResponseType(typeof(ConditionObservation), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateSurgeryObservation([FromBody] SurgeryObservationDto dto)
    {
        return await CreateObservation(dto, "专科护理", "外科");
    }

    /// <summary>
    /// 创建症状记录
    /// </summary>
    [HttpPost("symptoms")]
    [Authorize(Roles = "Admin,Nurse,HeadNurse")]
    [ProducesResponseType(typeof(ConditionObservation), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateSymptomRecord([FromBody] SymptomRecordDto dto)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized();
        }

        var patient = await _context.Patients.FindAsync(dto.PatientId);
        if (patient == null)
        {
            return NotFound(new { message = "患者不存在" });
        }

        var observation = new ConditionObservation
        {
            Id = Guid.NewGuid(),
            PatientId = dto.PatientId,
            ObservedByUserId = userId,
            ObservationType = "症状记录",
            Category = "症状",
            Content = BuildSymptomContent(dto),
            HasNausea = dto.HasNausea,
            HasVomiting = dto.HasVomiting,
            PainLevel = dto.PainLevel,
            ConsciousnessState = dto.ConsciousnessState,
            SleepQuality = dto.SleepQuality,
            ObservedAt = DateTime.UtcNow,
            Notes = dto.Notes,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = User.Identity?.Name
        };

        _context.ConditionObservations.Add(observation);
        await _context.SaveChangesAsync();

        await _context.Entry(observation).Reference(o => o.ObservedBy).LoadAsync();

        return CreatedAtAction(nameof(GetObservationsByPatient), new { patientId = dto.PatientId }, observation);
    }

    /// <summary>
    /// 通用创建观察记录方法
    /// </summary>
    private async Task<IActionResult> CreateObservation(CreateObservationBaseDto dto, string observationType, string category)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized();
        }

        var patient = await _context.Patients.FindAsync(dto.PatientId);
        if (patient == null)
        {
            return NotFound(new { message = "患者不存在" });
        }

        var observation = new ConditionObservation
        {
            Id = Guid.NewGuid(),
            PatientId = dto.PatientId,
            ObservedByUserId = userId,
            ObservationType = observationType,
            Category = category,
            Content = dto.Content,
            ObservedAt = DateTime.UtcNow,
            Notes = dto.Notes,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = User.Identity?.Name
        };

        // 根据类型设置特定字段
        if (dto is IcuObservationDto icuDto)
        {
            observation.VentilatorParameters = icuDto.VentilatorParameters != null 
                ? JsonSerializer.Serialize(icuDto.VentilatorParameters) 
                : null;
            observation.Hemodynamics = icuDto.Hemodynamics != null 
                ? JsonSerializer.Serialize(icuDto.Hemodynamics) 
                : null;
        }
        else if (dto is InternalMedicineObservationDto imDto)
        {
            observation.BloodGlucoseTrend = imDto.BloodGlucoseTrend;
            observation.BloodPressureTrend = imDto.BloodPressureTrend != null 
                ? JsonSerializer.Serialize(imDto.BloodPressureTrend) 
                : null;
        }
        else if (dto is SurgeryObservationDto surgeryDto)
        {
            observation.WoundHealing = surgeryDto.WoundHealing;
            observation.Drainage = surgeryDto.Drainage;
        }

        _context.ConditionObservations.Add(observation);
        await _context.SaveChangesAsync();

        await _context.Entry(observation).Reference(o => o.ObservedBy).LoadAsync();

        return CreatedAtAction(nameof(GetObservationsByPatient), new { patientId = dto.PatientId }, observation);
    }

    private string BuildSymptomContent(SymptomRecordDto dto)
    {
        var symptoms = new List<string>();
        if (dto.HasNausea) symptoms.Add("恶心");
        if (dto.HasVomiting) symptoms.Add("呕吐");
        if (dto.PainLevel.HasValue) symptoms.Add($"疼痛等级：{dto.PainLevel}/10");
        if (!string.IsNullOrEmpty(dto.ConsciousnessState)) symptoms.Add($"意识状态：{dto.ConsciousnessState}");
        if (!string.IsNullOrEmpty(dto.SleepQuality)) symptoms.Add($"睡眠质量：{dto.SleepQuality}");

        return string.Join("；", symptoms);
    }
}

/// <summary>
/// 创建观察记录基础DTO
/// </summary>
public class CreateObservationBaseDto
{
    public Guid PatientId { get; set; }
    public string Content { get; set; } = string.Empty;
    public string? Notes { get; set; }
}

/// <summary>
/// ICU观察DTO
/// </summary>
public class IcuObservationDto : CreateObservationBaseDto
{
    public Dictionary<string, object>? VentilatorParameters { get; set; } // 呼吸机参数
    public Dictionary<string, object>? Hemodynamics { get; set; } // 血流动力学
}

/// <summary>
/// 内科观察DTO
/// </summary>
public class InternalMedicineObservationDto : CreateObservationBaseDto
{
    public decimal? BloodGlucoseTrend { get; set; } // 血糖趋势
    public Dictionary<string, object>? BloodPressureTrend { get; set; } // 血压趋势
}

/// <summary>
/// 外科观察DTO
/// </summary>
public class SurgeryObservationDto : CreateObservationBaseDto
{
    public string? WoundHealing { get; set; } // 伤口愈合情况
    public string? Drainage { get; set; } // 引流情况
}

/// <summary>
/// 症状记录DTO
/// </summary>
public class SymptomRecordDto
{
    public Guid PatientId { get; set; }
    public bool HasNausea { get; set; }
    public bool HasVomiting { get; set; }
    public int? PainLevel { get; set; } // 0-10
    public string? ConsciousnessState { get; set; } // 意识状态
    public string? SleepQuality { get; set; } // 睡眠质量
    public string? Notes { get; set; }
}

