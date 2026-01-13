using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NisSystem.API.Data;
using NisSystem.API.Models;
using System.Security.Claims;

namespace NisSystem.API.Controllers;

/// <summary>
/// 用药效果观察控制器
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class MedicationEffectsController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<MedicationEffectsController> _logger;

    public MedicationEffectsController(ApplicationDbContext context, ILogger<MedicationEffectsController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// 获取用药效果记录
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<MedicationEffect>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetEffects([FromQuery] Guid? medicationExecutionId, [FromQuery] Guid? patientId)
    {
        var query = _context.MedicationEffects
            .Include(e => e.MedicationExecution)
            .ThenInclude(ex => ex.MedicationOrder)
            .Include(e => e.ObservedBy)
            .AsQueryable();

        if (medicationExecutionId.HasValue)
        {
            query = query.Where(e => e.MedicationExecutionId == medicationExecutionId.Value);
        }

        if (patientId.HasValue)
        {
            query = query.Where(e => e.MedicationExecution.MedicationOrder.PatientId == patientId.Value);
        }

        var effects = await query
            .OrderByDescending(e => e.ObservedAt)
            .ToListAsync();

        return Ok(effects);
    }

    /// <summary>
    /// 创建用药效果观察
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Admin,Nurse,HeadNurse")]
    [ProducesResponseType(typeof(MedicationEffect), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateEffect([FromBody] CreateMedicationEffectDto dto)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized();
        }

        var execution = await _context.MedicationExecutions
            .Include(e => e.MedicationOrder)
            .FirstOrDefaultAsync(e => e.Id == dto.MedicationExecutionId);

        if (execution == null)
        {
            return NotFound(new { message = "用药执行记录不存在" });
        }

        var effect = new MedicationEffect
        {
            Id = Guid.NewGuid(),
            MedicationExecutionId = dto.MedicationExecutionId,
            ObservedByUserId = userId,
            EffectType = dto.EffectType,
            Observation = dto.Observation,
            HasAdverseReaction = dto.HasAdverseReaction,
            AdverseReactionDetails = dto.AdverseReactionDetails,
            ObservedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = User.Identity?.Name
        };

        _context.MedicationEffects.Add(effect);
        await _context.SaveChangesAsync();

        await _context.Entry(effect).Reference(e => e.MedicationExecution).LoadAsync();
        await _context.Entry(effect).Reference(e => e.ObservedBy).LoadAsync();

        return CreatedAtAction(nameof(GetEffects), new { id = effect.Id }, effect);
    }

    /// <summary>
    /// 获取不良反应记录
    /// </summary>
    [HttpGet("adverse-reactions")]
    [Authorize(Roles = "Admin,Nurse,HeadNurse,Doctor")]
    [ProducesResponseType(typeof(List<MedicationEffect>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAdverseReactions([FromQuery] Guid? patientId, [FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate)
    {
        var query = _context.MedicationEffects
            .Include(e => e.MedicationExecution)
            .ThenInclude(ex => ex.MedicationOrder)
            .ThenInclude(o => o.Patient)
            .Where(e => e.HasAdverseReaction)
            .AsQueryable();

        if (patientId.HasValue)
        {
            query = query.Where(e => e.MedicationExecution.MedicationOrder.PatientId == patientId.Value);
        }

        if (startDate.HasValue)
        {
            query = query.Where(e => e.ObservedAt >= startDate.Value);
        }

        if (endDate.HasValue)
        {
            query = query.Where(e => e.ObservedAt <= endDate.Value);
        }

        var reactions = await query
            .OrderByDescending(e => e.ObservedAt)
            .ToListAsync();

        return Ok(reactions);
    }
}

/// <summary>
/// 创建用药效果DTO
/// </summary>
public class CreateMedicationEffectDto
{
    public Guid MedicationExecutionId { get; set; }
    public string EffectType { get; set; } = string.Empty; // 有效/无效/不良反应
    public string? Observation { get; set; }
    public bool HasAdverseReaction { get; set; }
    public string? AdverseReactionDetails { get; set; }
}

