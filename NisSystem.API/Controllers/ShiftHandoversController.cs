using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NisSystem.API.Data;
using NisSystem.API.Models;
using System.Security.Claims;

namespace NisSystem.API.Controllers;

/// <summary>
/// 交接班管理控制器
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ShiftHandoversController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ShiftHandoversController> _logger;

    public ShiftHandoversController(ApplicationDbContext context, ILogger<ShiftHandoversController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// 获取交接班记录列表
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<ShiftHandover>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetHandovers([FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate)
    {
        var query = _context.ShiftHandovers
            .Include(h => h.HandoverFrom)
            .Include(h => h.HandoverTo)
            .AsQueryable();

        if (startDate.HasValue)
        {
            query = query.Where(h => h.ShiftDate >= startDate.Value);
        }

        if (endDate.HasValue)
        {
            query = query.Where(h => h.ShiftDate <= endDate.Value);
        }

        var handovers = await query
            .OrderByDescending(h => h.ShiftDate)
            .ToListAsync();

        return Ok(handovers);
    }

    /// <summary>
    /// 创建交接班记录
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Admin,Nurse,HeadNurse")]
    [ProducesResponseType(typeof(ShiftHandover), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateHandover([FromBody] CreateShiftHandoverDto dto)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized();
        }

        // 验证接班人存在
        var handoverTo = await _context.Users.FindAsync(dto.HandoverToUserId);
        if (handoverTo == null)
        {
            return NotFound(new { message = "接班人不存在" });
        }

        var handover = new ShiftHandover
        {
            Id = Guid.NewGuid(),
            HandoverFromUserId = userId,
            HandoverToUserId = dto.HandoverToUserId,
            ShiftDate = dto.ShiftDate ?? DateTime.UtcNow,
            ShiftType = dto.ShiftType,
            Content = dto.Content != null ? JsonSerializer.Serialize(dto.Content) : string.Empty,
            ImportantNotes = dto.ImportantNotes,
            UnfinishedTasks = dto.UnfinishedTasks,
            EquipmentHandover = dto.EquipmentHandover,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = User.Identity?.Name
        };

        _context.ShiftHandovers.Add(handover);
        await _context.SaveChangesAsync();

        await _context.Entry(handover).Reference(h => h.HandoverFrom).LoadAsync();
        await _context.Entry(handover).Reference(h => h.HandoverTo).LoadAsync();

        return CreatedAtAction(nameof(GetHandovers), new { id = handover.Id }, handover);
    }

    /// <summary>
    /// 获取待交接事项（未完成任务提醒）
    /// </summary>
    [HttpGet("pending-tasks")]
    [Authorize(Roles = "Admin,Nurse,HeadNurse")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPendingTasks()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized();
        }

        // 获取未完成的医嘱执行
        var pendingMedications = await _context.MedicationExecutions
            .Include(e => e.MedicationOrder)
            .ThenInclude(o => o.Patient)
            .Where(e => e.ExecutedByUserId == userId && 
                       e.Status == "待执行" &&
                       e.ScheduledTime <= DateTime.UtcNow.AddHours(2))
            .Select(e => new
            {
                Type = "用药执行",
                PatientName = e.MedicationOrder.Patient.Name,
                Description = $"{e.MedicationOrder.MedicationName} - {e.ScheduledTime:HH:mm}",
                ScheduledTime = e.ScheduledTime
            })
            .ToListAsync();

        // 获取到期的护理评估
        var dueAssessments = await _context.NursingAssessments
            .Include(a => a.Patient)
            .Where(a => a.AssessedByUserId == userId &&
                       a.NextAssessmentDate.HasValue &&
                       a.NextAssessmentDate <= DateTime.UtcNow.AddDays(1))
            .Select(a => new
            {
                Type = "护理评估",
                PatientName = a.Patient.Name,
                Description = $"{a.AssessmentType}评估到期",
                ScheduledTime = a.NextAssessmentDate
            })
            .ToListAsync();

        var result = new
        {
            PendingMedications = pendingMedications,
            DueAssessments = dueAssessments,
            TotalCount = pendingMedications.Count + dueAssessments.Count
        };

        return Ok(result);
    }

    /// <summary>
    /// 获取重点关注患者列表
    /// </summary>
    [HttpGet("high-risk-patients")]
    [Authorize(Roles = "Admin,Nurse,HeadNurse")]
    [ProducesResponseType(typeof(List<object>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetHighRiskPatients()
    {
        // 获取高风险评估的患者
        var highRiskPatients = await _context.NursingAssessments
            .Include(a => a.Patient)
            .Where(a => a.RiskLevel == "高风险")
            .GroupBy(a => a.PatientId)
            .Select(g => new
            {
                PatientId = g.Key,
                PatientName = g.First().Patient.Name,
                RiskTypes = g.Select(a => a.AssessmentType).ToList(),
                LatestAssessment = g.OrderByDescending(a => a.AssessedAt).First().AssessedAt
            })
            .ToListAsync();

        return Ok(highRiskPatients);
    }
}

/// <summary>
/// 创建交接班DTO
/// </summary>
public class CreateShiftHandoverDto
{
    public Guid HandoverToUserId { get; set; }
    public DateTime? ShiftDate { get; set; }
    public string ShiftType { get; set; } = string.Empty; // 白班/夜班
    public ShiftHandoverContent? Content { get; set; }
    public string? ImportantNotes { get; set; }
    public string? UnfinishedTasks { get; set; }
    public string? EquipmentHandover { get; set; }
}

/// <summary>
/// 交接班内容
/// </summary>
public class ShiftHandoverContent
{
    public List<PatientHandoverInfo>? Patients { get; set; }
    public List<string>? SpecialNotes { get; set; }
}

/// <summary>
/// 患者交接信息
/// </summary>
public class PatientHandoverInfo
{
    public Guid PatientId { get; set; }
    public string PatientName { get; set; } = string.Empty;
    public string Condition { get; set; } = string.Empty;
    public string? SpecialCare { get; set; }
}

