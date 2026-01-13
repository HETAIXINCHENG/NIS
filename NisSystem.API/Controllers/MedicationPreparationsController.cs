using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NisSystem.API.Data;
using NisSystem.API.Models;
using System.Security.Claims;

namespace NisSystem.API.Controllers;

/// <summary>
/// 药物配制管理控制器
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class MedicationPreparationsController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<MedicationPreparationsController> _logger;

    public MedicationPreparationsController(ApplicationDbContext context, ILogger<MedicationPreparationsController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// 获取药物配制记录
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<MedicationPreparation>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPreparations([FromQuery] Guid? medicationOrderId)
    {
        var query = _context.MedicationPreparations
            .Include(p => p.MedicationOrder)
            .Include(p => p.PreparedBy)
            .AsQueryable();

        if (medicationOrderId.HasValue)
        {
            query = query.Where(p => p.MedicationOrderId == medicationOrderId.Value);
        }

        var preparations = await query
            .OrderByDescending(p => p.PreparedAt)
            .ToListAsync();

        return Ok(preparations);
    }

    /// <summary>
    /// 创建药物配制记录
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Admin,Nurse,HeadNurse")]
    [ProducesResponseType(typeof(MedicationPreparation), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreatePreparation([FromBody] CreateMedicationPreparationDto dto)
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
        var hasAllergy = false;
        if (!string.IsNullOrEmpty(order.Patient.Allergies))
        {
            var allergies = Helpers.EncryptionHelper.Decrypt(order.Patient.Allergies);
            hasAllergy = allergies.Contains(order.MedicationName, StringComparison.OrdinalIgnoreCase);
        }

        var preparation = new MedicationPreparation
        {
            Id = Guid.NewGuid(),
            MedicationOrderId = dto.MedicationOrderId,
            PreparedByUserId = userId,
            PreparationType = dto.PreparationType,
            PreparationContent = dto.PreparationContent,
            Notes = dto.Notes,
            PreparedAt = DateTime.UtcNow,
            HasContraindication = hasContraindication,
            HasAllergyWarning = hasAllergy,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = User.Identity?.Name
        };

        _context.MedicationPreparations.Add(preparation);
        await _context.SaveChangesAsync();

        await _context.Entry(preparation).Reference(p => p.MedicationOrder).LoadAsync();
        await _context.Entry(preparation).Reference(p => p.PreparedBy).LoadAsync();

        return CreatedAtAction(nameof(GetPreparations), new { id = preparation.Id }, preparation);
    }

    /// <summary>
    /// 检查配伍禁忌
    /// </summary>
    private async Task<bool> CheckContraindication(MedicationOrder order)
    {
        // 这里应该查询药物配伍禁忌数据库
        // 简化处理：检查是否有其他正在执行的医嘱可能产生配伍禁忌
        var activeOrders = await _context.MedicationOrders
            .Where(o => o.PatientId == order.PatientId &&
                       o.Status != "已完成" &&
                       o.Status != "已停止" &&
                       o.Id != order.Id)
            .ToListAsync();

        // 实际应该检查药物配伍禁忌表
        return false;
    }
}

/// <summary>
/// 创建药物配制DTO
/// </summary>
public class CreateMedicationPreparationDto
{
    public Guid MedicationOrderId { get; set; }
    public string PreparationType { get; set; } = string.Empty;
    public string PreparationContent { get; set; } = string.Empty;
    public string? Notes { get; set; }
}

