using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NisSystem.API.Data;
using NisSystem.API.Models;
using System.Security.Claims;

namespace NisSystem.API.Controllers;

/// <summary>
/// 护理措施记录控制器
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class NursingRecordsController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<NursingRecordsController> _logger;

    public NursingRecordsController(ApplicationDbContext context, ILogger<NursingRecordsController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// 获取患者的所有护理记录
    /// </summary>
    [HttpGet("patient/{patientId}")]
    [ProducesResponseType(typeof(List<NursingRecord>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRecordsByPatient(
        Guid patientId,
        [FromQuery] string? recordType,
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate)
    {
        var query = _context.NursingRecords
            .Include(r => r.RecordedBy)
            .Where(r => r.PatientId == patientId)
            .AsQueryable();

        if (!string.IsNullOrEmpty(recordType))
        {
            query = query.Where(r => r.RecordType == recordType);
        }

        if (startDate.HasValue)
        {
            query = query.Where(r => r.RecordedAt >= startDate.Value);
        }

        if (endDate.HasValue)
        {
            query = query.Where(r => r.RecordedAt <= endDate.Value);
        }

        var records = await query
            .OrderByDescending(r => r.RecordedAt)
            .ToListAsync();

        // 清除循环引用
        foreach (var record in records)
        {
            if (record.Patient != null)
            {
                record.Patient.NursingRecords = new List<NursingRecord>();
            }
        }

        return Ok(records);
    }

    /// <summary>
    /// 创建护理措施记录（临床护理记录单格式）
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Admin,Nurse,HeadNurse")]
    [ProducesResponseType(typeof(NursingRecord), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateNursingRecord([FromBody] CreateNursingRecordDto dto)
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

        // 解析日期和时间
        DateTime recordedAt = DateTime.UtcNow;
        if (!string.IsNullOrEmpty(dto.RecordDate) && !string.IsNullOrEmpty(dto.RecordTime))
        {
            if (DateTime.TryParse($"{dto.RecordDate} {dto.RecordTime}", out var parsedDate))
            {
                recordedAt = DateTime.SpecifyKind(parsedDate, DateTimeKind.Utc);
            }
        }

        var record = new NursingRecord
        {
            Id = Guid.NewGuid(),
            PatientId = dto.PatientId,
            RecordedByUserId = userId,
            RecordType = "临床护理记录",
            Content = dto.ConditionAndMeasures ?? string.Empty,
            RecordedAt = recordedAt,
            Temperature = dto.Temperature,
            Pulse = dto.Pulse,
            Respiration = dto.Respiration,
            BloodPressure = dto.BloodPressure,
            IntakeName = dto.IntakeName,
            IntakeVolume = dto.IntakeVolume,
            IntakeRoute = dto.IntakeRoute,
            OutputName = dto.OutputName,
            OutputVolume = dto.OutputVolume,
            ConditionAndMeasures = dto.ConditionAndMeasures,
            CreatedAt = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc),
            CreatedBy = User.Identity?.Name
        };

        _context.NursingRecords.Add(record);
        await _context.SaveChangesAsync();

        await _context.Entry(record).Reference(r => r.RecordedBy).LoadAsync();
        if (record.Patient != null)
        {
            record.Patient.NursingRecords = new List<NursingRecord>();
        }

        return CreatedAtAction(nameof(GetRecordsByPatient), new { patientId = dto.PatientId }, record);
    }

    /// <summary>
    /// 更新护理措施记录
    /// </summary>
    [HttpPut("{id}")]
    [Authorize(Roles = "Admin,Nurse,HeadNurse")]
    [ProducesResponseType(typeof(NursingRecord), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateNursingRecord(Guid id, [FromBody] CreateNursingRecordDto dto)
    {
        var record = await _context.NursingRecords.FindAsync(id);
        if (record == null)
        {
            return NotFound();
        }

        // 解析日期和时间
        if (!string.IsNullOrEmpty(dto.RecordDate) && !string.IsNullOrEmpty(dto.RecordTime))
        {
            if (DateTime.TryParse($"{dto.RecordDate} {dto.RecordTime}", out var parsedDate))
            {
                record.RecordedAt = DateTime.SpecifyKind(parsedDate, DateTimeKind.Utc);
            }
        }

        record.Temperature = dto.Temperature;
        record.Pulse = dto.Pulse;
        record.Respiration = dto.Respiration;
        record.BloodPressure = dto.BloodPressure;
        record.IntakeName = dto.IntakeName;
        record.IntakeVolume = dto.IntakeVolume;
        record.IntakeRoute = dto.IntakeRoute;
        record.OutputName = dto.OutputName;
        record.OutputVolume = dto.OutputVolume;
        record.ConditionAndMeasures = dto.ConditionAndMeasures;
        record.Content = dto.ConditionAndMeasures ?? string.Empty;
        record.UpdatedAt = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc);
        record.UpdatedBy = User.Identity?.Name;

        await _context.SaveChangesAsync();

        await _context.Entry(record).Reference(r => r.RecordedBy).LoadAsync();
        if (record.Patient != null)
        {
            record.Patient.NursingRecords = new List<NursingRecord>();
        }

        return Ok(record);
    }

    /// <summary>
    /// 删除护理措施记录
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin,Nurse,HeadNurse")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteNursingRecord(Guid id)
    {
        var record = await _context.NursingRecords.FindAsync(id);
        if (record == null)
        {
            return NotFound();
        }

        _context.NursingRecords.Remove(record);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    /// <summary>
    /// 创建用药执行记录
    /// </summary>
    [HttpPost("medication")]
    [Authorize(Roles = "Admin,Nurse,HeadNurse")]
    [ProducesResponseType(typeof(NursingRecord), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateMedicationRecord([FromBody] CreateMedicationRecordDto dto)
    {
        return await CreateRecord(dto, "用药执行");
    }

    /// <summary>
    /// 创建输液记录
    /// </summary>
    [HttpPost("infusion")]
    [Authorize(Roles = "Admin,Nurse,HeadNurse")]
    [ProducesResponseType(typeof(NursingRecord), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateInfusionRecord([FromBody] CreateInfusionRecordDto dto)
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

        var record = new NursingRecord
        {
            Id = Guid.NewGuid(),
            PatientId = dto.PatientId,
            RecordedByUserId = userId,
            RecordType = "输液",
            Content = $"输液记录：滴速 {dto.InfusionRate} 滴/分，总量 {dto.TotalVolume} ml",
            InfusionRate = dto.InfusionRate,
            TotalVolume = dto.TotalVolume,
            RecordedAt = DateTime.UtcNow,
            Notes = dto.Notes,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = User.Identity?.Name
        };

        _context.NursingRecords.Add(record);
        await _context.SaveChangesAsync();

        await _context.Entry(record).Reference(r => r.RecordedBy).LoadAsync();

        return CreatedAtAction(nameof(GetRecordsByPatient), new { patientId = dto.PatientId }, record);
    }

    /// <summary>
    /// 创建伤口护理记录
    /// </summary>
    [HttpPost("wound-care")]
    [Authorize(Roles = "Admin,Nurse,HeadNurse")]
    [ProducesResponseType(typeof(NursingRecord), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateWoundCareRecord([FromBody] CreateWoundCareRecordDto dto)
    {
        return await CreateRecord(dto, "伤口护理");
    }

    /// <summary>
    /// 创建导管护理记录
    /// </summary>
    [HttpPost("catheter-care")]
    [Authorize(Roles = "Admin,Nurse,HeadNurse")]
    [ProducesResponseType(typeof(NursingRecord), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateCatheterCareRecord([FromBody] CreateCatheterCareRecordDto dto)
    {
        return await CreateRecord(dto, "导管护理");
    }

    /// <summary>
    /// 通用创建记录方法
    /// </summary>
    private async Task<IActionResult> CreateRecord(CreateRecordBaseDto dto, string recordType)
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

        var record = new NursingRecord
        {
            Id = Guid.NewGuid(),
            PatientId = dto.PatientId,
            RecordedByUserId = userId,
            RecordType = recordType,
            Content = dto.Content,
            MedicationName = dto is CreateMedicationRecordDto medDto ? medDto.MedicationName : null,
            Dosage = dto is CreateMedicationRecordDto medDto2 ? medDto2.Dosage : null,
            Route = dto is CreateMedicationRecordDto medDto3 ? medDto3.Route : null,
            RecordedAt = DateTime.UtcNow,
            Notes = dto.Notes,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = User.Identity?.Name
        };

        _context.NursingRecords.Add(record);
        await _context.SaveChangesAsync();

        await _context.Entry(record).Reference(r => r.RecordedBy).LoadAsync();

        return CreatedAtAction(nameof(GetRecordsByPatient), new { patientId = dto.PatientId }, record);
    }
}

/// <summary>
/// 创建记录基础DTO
/// </summary>
public class CreateRecordBaseDto
{
    public Guid PatientId { get; set; }
    public string Content { get; set; } = string.Empty;
    public string? Notes { get; set; }
}

/// <summary>
/// 创建用药记录DTO
/// </summary>
public class CreateMedicationRecordDto : CreateRecordBaseDto
{
    public string MedicationName { get; set; } = string.Empty;
    public string Dosage { get; set; } = string.Empty;
    public string Route { get; set; } = string.Empty; // 给药途径
}

/// <summary>
/// 创建输液记录DTO
/// </summary>
public class CreateInfusionRecordDto : CreateRecordBaseDto
{
    public decimal InfusionRate { get; set; } // 滴速
    public decimal TotalVolume { get; set; } // 总量
}

/// <summary>
/// 创建伤口护理记录DTO
/// </summary>
public class CreateWoundCareRecordDto : CreateRecordBaseDto
{
    public string WoundLocation { get; set; } = string.Empty;
    public string WoundCondition { get; set; } = string.Empty;
}

/// <summary>
/// 创建导管护理记录DTO
/// </summary>
public class CreateCatheterCareRecordDto : CreateRecordBaseDto
{
    public string CatheterType { get; set; } = string.Empty; // 留置针、导尿管、胃管等
    public string CareContent { get; set; } = string.Empty;
}

/// <summary>
/// 创建护理措施记录DTO（临床护理记录单格式）
/// </summary>
public class CreateNursingRecordDto
{
    public Guid PatientId { get; set; }
    public string RecordDate { get; set; } = string.Empty; // YYYY-MM-DD
    public string RecordTime { get; set; } = string.Empty; // HH:mm
    public decimal? Temperature { get; set; }
    public int? Pulse { get; set; }
    public int? Respiration { get; set; }
    public string? BloodPressure { get; set; }
    public string? IntakeName { get; set; }
    public decimal? IntakeVolume { get; set; }
    public string? IntakeRoute { get; set; }
    public string? OutputName { get; set; }
    public decimal? OutputVolume { get; set; }
    public string? ConditionAndMeasures { get; set; }
}

