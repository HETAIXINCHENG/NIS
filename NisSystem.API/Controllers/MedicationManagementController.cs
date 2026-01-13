using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NisSystem.API.Data;
using NisSystem.API.Helpers;
using NisSystem.API.Models;
using System.Security.Claims;

namespace NisSystem.API.Controllers;

/// <summary>
/// 用药管理控制器
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class MedicationManagementController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<MedicationManagementController> _logger;

    public MedicationManagementController(ApplicationDbContext context, ILogger<MedicationManagementController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// 药物准备/配制记录
    /// </summary>
    [HttpPost("prepare")]
    [Authorize(Roles = "Admin,Nurse,HeadNurse")]
    [ProducesResponseType(typeof(MedicationPreparation), StatusCodes.Status201Created)]
    public async Task<IActionResult> PrepareMedication([FromBody] PrepareMedicationDto dto)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized();
        }

        var order = await _context.MedicationOrders
            .Include(o => o.Patient)
            .FirstOrDefaultAsync(o => o.Id == dto.MedicationOrderId);

        if (order == null)
        {
            return NotFound(new { message = "医嘱不存在" });
        }

        // 检查配伍禁忌
        var hasContraindication = await CheckContraindication(order);

        // 检查药物过敏
        var hasAllergyWarning = false;
        if (!string.IsNullOrEmpty(order.Patient.Allergies))
        {
            var allergies = EncryptionHelper.Decrypt(order.Patient.Allergies);
            hasAllergyWarning = allergies.Contains(order.MedicationName, StringComparison.OrdinalIgnoreCase);
        }

        var preparation = new MedicationPreparation
        {
            Id = Guid.NewGuid(),
            MedicationOrderId = dto.MedicationOrderId,
            PreparedByUserId = userId,
            PreparationType = dto.PreparationType,
            PreparationContent = dto.PreparationContent,
            PreparedAt = DateTime.UtcNow,
            HasContraindication = hasContraindication,
            HasAllergyWarning = hasAllergyWarning,
            Notes = dto.Notes,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = User.Identity?.Name
        };

        _context.MedicationPreparations.Add(preparation);
        await _context.SaveChangesAsync();

        await _context.Entry(preparation).Reference(p => p.PreparedBy).LoadAsync();

        return CreatedAtAction(nameof(GetPreparations), new { orderId = dto.MedicationOrderId }, preparation);
    }

    /// <summary>
    /// 获取药物准备记录
    /// </summary>
    [HttpGet("preparations/{orderId}")]
    [ProducesResponseType(typeof(List<MedicationPreparation>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPreparations(Guid orderId)
    {
        var preparations = await _context.MedicationPreparations
            .Include(p => p.PreparedBy)
            .Where(p => p.MedicationOrderId == orderId)
            .OrderByDescending(p => p.PreparedAt)
            .ToListAsync();

        return Ok(preparations);
    }

    /// <summary>
    /// 剂量计算辅助
    /// </summary>
    [HttpPost("calculate-dosage")]
    [Authorize(Roles = "Admin,Nurse,HeadNurse")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<IActionResult> CalculateDosage([FromBody] DosageCalculationDto dto)
    {
        // 剂量计算逻辑（根据体重、体表面积等）
        decimal calculatedDosage = 0;

        if (dto.CalculationType == "按体重")
        {
            calculatedDosage = (dto.Weight ?? 0) * (dto.DosagePerKg ?? 0);
        }
        else if (dto.CalculationType == "按体表面积")
        {
            var height = dto.Height ?? 0;
            var weight = dto.Weight ?? 0;
            var bsa = Math.Sqrt((double)(height * weight) / 3600); // 简化计算
            calculatedDosage = (decimal)bsa * (dto.DosagePerBSA ?? 0);
        }
        else if (dto.CalculationType == "固定剂量")
        {
            calculatedDosage = dto.FixedDosage ?? 0;
        }

        var result = new
        {
            CalculatedDosage = Math.Round(calculatedDosage, 2),
            Unit = dto.Unit,
            CalculationType = dto.CalculationType,
            Notes = $"根据{dto.CalculationType}计算得出"
        };

        return Ok(result);
    }

    /// <summary>
    /// 用药效果观察
    /// </summary>
    [HttpPost("effect")]
    [Authorize(Roles = "Admin,Nurse,HeadNurse")]
    [ProducesResponseType(typeof(MedicationEffect), StatusCodes.Status201Created)]
    public async Task<IActionResult> RecordEffect([FromBody] MedicationEffectDto dto)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized();
        }

        var execution = await _context.MedicationExecutions.FindAsync(dto.MedicationExecutionId);
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
            ObservedAt = DateTime.UtcNow,
            HasAdverseReaction = dto.HasAdverseReaction,
            AdverseReactionDetails = dto.AdverseReactionDetails,
            Notes = dto.Notes,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = User.Identity?.Name
        };

        _context.MedicationEffects.Add(effect);
        await _context.SaveChangesAsync();

        await _context.Entry(effect).Reference(e => e.ObservedBy).LoadAsync();

        return CreatedAtAction(nameof(GetEffects), new { executionId = dto.MedicationExecutionId }, effect);
    }

    /// <summary>
    /// 获取用药效果记录
    /// </summary>
    [HttpGet("effects/{executionId}")]
    [ProducesResponseType(typeof(List<MedicationEffect>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetEffects(Guid executionId)
    {
        var effects = await _context.MedicationEffects
            .Include(e => e.ObservedBy)
            .Where(e => e.MedicationExecutionId == executionId)
            .OrderByDescending(e => e.ObservedAt)
            .ToListAsync();

        return Ok(effects);
    }

    /// <summary>
    /// 检查配伍禁忌
    /// </summary>
    private async Task<bool> CheckContraindication(MedicationOrder order)
    {
        // 检查是否有其他正在执行的医嘱存在配伍禁忌
        var activeOrders = await _context.MedicationOrders
            .Where(o => o.PatientId == order.PatientId &&
                       o.Status != "已完成" &&
                       o.Status != "已停止" &&
                       o.Id != order.Id)
            .ToListAsync();

        // 实际应该查询药物配伍禁忌表
        // 这里简化处理，检查是否有其他药物同时使用
        return activeOrders.Any();
    }
}

/// <summary>
/// 药物准备DTO
/// </summary>
public class PrepareMedicationDto
{
    public Guid MedicationOrderId { get; set; }
    public string PreparationType { get; set; } = "配制"; // 配制、准备
    public string PreparationContent { get; set; } = string.Empty;
    public string? Notes { get; set; }
}

/// <summary>
/// 剂量计算DTO
/// </summary>
public class DosageCalculationDto
{
    public string CalculationType { get; set; } = string.Empty; // 按体重、按体表面积、固定剂量
    public decimal? Weight { get; set; } // 体重（kg）
    public decimal? Height { get; set; } // 身高（cm）
    public decimal? DosagePerKg { get; set; } // 每公斤剂量
    public decimal? DosagePerBSA { get; set; } // 每平方米体表面积剂量
    public decimal? FixedDosage { get; set; } // 固定剂量
    public string Unit { get; set; } = "mg"; // 单位
}

/// <summary>
/// 用药效果DTO
/// </summary>
public class MedicationEffectDto
{
    public Guid MedicationExecutionId { get; set; }
    public string EffectType { get; set; } = string.Empty; // 有效、无效、不良反应
    public string Observation { get; set; } = string.Empty;
    public bool HasAdverseReaction { get; set; }
    public string? AdverseReactionDetails { get; set; }
    public string? Notes { get; set; }
}

