using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NisSystem.API.Data;
using NisSystem.API.DTOs;
using NisSystem.API.Models;
using System.Security.Claims;

namespace NisSystem.API.Controllers;

/// <summary>
/// 用药执行记录控制器
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class MedicationExecutionsController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<MedicationExecutionsController> _logger;

    public MedicationExecutionsController(ApplicationDbContext context, ILogger<MedicationExecutionsController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// 获取执行记录列表（支持按患者、医嘱类型、状态、日期筛选）
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<MedicationExecution>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetExecutions(
        [FromQuery] Guid? patientId,
        [FromQuery] string? orderType,
        [FromQuery] string? status,
        [FromQuery] string? executionDate,
        [FromQuery] string? department)
    {
        var query = _context.MedicationExecutions
            .Include(e => e.MedicationOrder)
                .ThenInclude(o => o.Patient)
            .Include(e => e.ExecutedBy)
            .Include(e => e.VerifiedBy)
            .AsQueryable();

        if (patientId.HasValue)
        {
            query = query.Where(e => e.MedicationOrder.PatientId == patientId.Value);
        }

        if (!string.IsNullOrEmpty(orderType))
        {
            query = query.Where(e => e.MedicationOrder.OrderType == orderType);
        }

        if (!string.IsNullOrEmpty(status))
        {
            query = query.Where(e => e.Status == status);
        }

        if (!string.IsNullOrEmpty(executionDate))
        {
            if (DateTime.TryParse(executionDate, out var date))
            {
                var startDate = DateTime.SpecifyKind(date.Date, DateTimeKind.Utc);
                var endDate = startDate.AddDays(1);
                query = query.Where(e => e.ExecutedTime.HasValue && 
                    e.ExecutedTime >= startDate && e.ExecutedTime < endDate);
            }
        }

        if (!string.IsNullOrEmpty(department))
        {
            query = query.Where(e => e.MedicationOrder.Patient.Department == department);
        }

        var executions = await query
            .OrderByDescending(e => e.ExecutedTime ?? e.ScheduledTime)
            .ToListAsync();

        return Ok(executions);
    }

    /// <summary>
    /// 获取单个执行记录
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(MedicationExecution), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetExecution(Guid id)
    {
        var execution = await _context.MedicationExecutions
            .Include(e => e.MedicationOrder)
                .ThenInclude(o => o.Patient)
            .Include(e => e.ExecutedBy)
            .Include(e => e.VerifiedBy)
            .FirstOrDefaultAsync(e => e.Id == id);

        if (execution == null)
        {
            return NotFound(new { message = "执行记录不存在" });
        }

        return Ok(execution);
    }

    /// <summary>
    /// 创建执行记录
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Admin,Nurse,HeadNurse")]
    [ProducesResponseType(typeof(MedicationExecution), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateExecution([FromBody] CreateMedicationExecutionDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(new { message = "无法获取用户ID，请重新登录" });
        }

        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == userId && !u.IsDeleted && u.IsActive);
        if (user == null)
        {
            return BadRequest(new { message = "用户不存在，请重新登录" });
        }

        var order = await _context.MedicationOrders
            .Include(o => o.Patient)
            .FirstOrDefaultAsync(o => o.Id == dto.MedicationOrderId);

        if (order == null)
        {
            return NotFound(new { message = "医嘱不存在" });
        }

        DateTime? executedTime = null;
        if (!string.IsNullOrEmpty(dto.ExecutedTime))
        {
            if (DateTime.TryParse(dto.ExecutedTime, out var parsedTime))
            {
                executedTime = DateTime.SpecifyKind(parsedTime, DateTimeKind.Utc);
            }
        }

        var execution = new MedicationExecution
        {
            Id = Guid.NewGuid(),
            MedicationOrderId = dto.MedicationOrderId,
            ExecutedByUserId = userId,
            ScheduledTime = order.ScheduledTime ?? order.StartDate ?? DateTime.UtcNow,
            ExecutedTime = executedTime ?? DateTime.UtcNow,
            Status = dto.Status ?? "已执行",
            Notes = dto.Notes,
            IsVerified = false,
            VerificationCode = dto.VerificationCode,
            ElectronicSignature = User.Identity?.Name,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = User.Identity?.Name,
        };

        _context.MedicationExecutions.Add(execution);

        // 更新医嘱状态
        var hasPendingExecutions = await _context.MedicationExecutions
            .AnyAsync(e => e.MedicationOrderId == dto.MedicationOrderId && e.Status == "待执行");

        if (!hasPendingExecutions)
        {
            order.Status = "已完成";
            order.UpdatedAt = DateTime.UtcNow;
            order.UpdatedBy = User.Identity?.Name;
        }
        else
        {
            order.Status = "执行中";
        }

        await _context.SaveChangesAsync();

        // 加载导航属性
        await _context.Entry(execution).Reference(e => e.MedicationOrder).LoadAsync();
        await _context.Entry(execution).Reference(e => e.ExecutedBy).LoadAsync();

        return CreatedAtAction(nameof(GetExecution), new { id = execution.Id }, execution);
    }

    /// <summary>
    /// 更新执行记录
    /// </summary>
    [HttpPut("{id}")]
    [Authorize(Roles = "Admin,Nurse,HeadNurse")]
    [ProducesResponseType(typeof(MedicationExecution), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateExecution(Guid id, [FromBody] UpdateMedicationExecutionDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var execution = await _context.MedicationExecutions
            .Include(e => e.MedicationOrder)
            .FirstOrDefaultAsync(e => e.Id == id);

        if (execution == null)
        {
            return NotFound(new { message = "执行记录不存在" });
        }

        if (!string.IsNullOrEmpty(dto.Status))
        {
            execution.Status = dto.Status;
        }

        if (dto.ExecutedTime != null)
        {
            if (DateTime.TryParse(dto.ExecutedTime, out var parsedTime))
            {
                execution.ExecutedTime = DateTime.SpecifyKind(parsedTime, DateTimeKind.Utc);
            }
        }

        if (dto.Notes != null)
        {
            execution.Notes = dto.Notes;
        }

        execution.UpdatedAt = DateTime.UtcNow;
        execution.UpdatedBy = User.Identity?.Name;

        await _context.SaveChangesAsync();

        // 加载导航属性
        await _context.Entry(execution).Reference(e => e.MedicationOrder).LoadAsync();
        await _context.Entry(execution).Reference(e => e.ExecutedBy).LoadAsync();
        await _context.Entry(execution).Reference(e => e.VerifiedBy).LoadAsync();

        return Ok(execution);
    }

    /// <summary>
    /// 核对执行记录
    /// </summary>
    [HttpPost("{id}/verify")]
    [Authorize(Roles = "Admin,Nurse,HeadNurse")]
    [ProducesResponseType(typeof(MedicationExecution), StatusCodes.Status200OK)]
    public async Task<IActionResult> VerifyExecution(Guid id, [FromBody] VerifyMedicationExecutionDto dto)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(new { message = "无法获取用户ID，请重新登录" });
        }

        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == userId && !u.IsDeleted && u.IsActive);
        if (user == null)
        {
            return BadRequest(new { message = "用户不存在，请重新登录" });
        }

        var execution = await _context.MedicationExecutions
            .Include(e => e.MedicationOrder)
            .FirstOrDefaultAsync(e => e.Id == id);

        if (execution == null)
        {
            return NotFound(new { message = "执行记录不存在" });
        }

        execution.IsVerified = true;
        execution.VerifiedByUserId = userId;
        execution.VerifiedTime = DateTime.UtcNow;
        execution.VerificationCode = dto.VerificationCode;
        execution.UpdatedAt = DateTime.UtcNow;
        execution.UpdatedBy = User.Identity?.Name;

        await _context.SaveChangesAsync();

        // 加载导航属性
        await _context.Entry(execution).Reference(e => e.MedicationOrder).LoadAsync();
        await _context.Entry(execution).Reference(e => e.ExecutedBy).LoadAsync();
        await _context.Entry(execution).Reference(e => e.VerifiedBy).LoadAsync();

        return Ok(execution);
    }

    /// <summary>
    /// 删除执行记录
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin,HeadNurse")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteExecution(Guid id)
    {
        var execution = await _context.MedicationExecutions.FindAsync(id);
        if (execution == null)
        {
            return NotFound(new { message = "执行记录不存在" });
        }

        _context.MedicationExecutions.Remove(execution);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}

