using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NisSystem.API.Data;
using NisSystem.API.DTOs;
using NisSystem.API.Helpers;
using NisSystem.API.Models;
using System.Security.Claims;

namespace NisSystem.API.Controllers;

/// <summary>
/// 医嘱管理控制器
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class MedicationOrdersController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<MedicationOrdersController> _logger;

    public MedicationOrdersController(ApplicationDbContext context, ILogger<MedicationOrdersController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// 获取患者的所有医嘱
    /// </summary>
    [HttpGet("patient/{patientId}")]
    [ProducesResponseType(typeof(List<MedicationOrder>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetOrdersByPatient(Guid patientId, [FromQuery] string? orderType, [FromQuery] string? status)
    {
        var query = _context.MedicationOrders
            .Include(o => o.Executions)
            .Where(o => o.PatientId == patientId)
            .AsQueryable();

        if (!string.IsNullOrEmpty(orderType))
        {
            query = query.Where(o => o.OrderType == orderType);
        }

        if (!string.IsNullOrEmpty(status))
        {
            query = query.Where(o => o.Status == status);
        }

        var orders = await query
            .OrderByDescending(o => o.OrderDate)
            .ToListAsync();

        return Ok(orders);
    }

    /// <summary>
    /// 获取待执行的医嘱
    /// </summary>
    [HttpGet("pending")]
    [Authorize(Roles = "Admin,Nurse,HeadNurse")]
    [ProducesResponseType(typeof(List<MedicationOrder>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPendingOrders([FromQuery] Guid? patientId)
    {
        var query = _context.MedicationOrders
            .Include(o => o.Patient)
            .Where(o => o.Status == "待执行" || o.Status == "执行中")
            .AsQueryable();

        if (patientId.HasValue)
        {
            query = query.Where(o => o.PatientId == patientId.Value);
        }

        var orders = await query
            .OrderBy(o => o.ScheduledTime ?? o.StartDate)
            .ToListAsync();

        return Ok(orders);
    }

    /// <summary>
    /// 创建医嘱
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Admin,Doctor,HeadNurse")]
    [ProducesResponseType(typeof(MedicationOrder), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateOrder([FromBody] CreateMedicationOrderDto dto)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState
                .Where(x => x.Value?.Errors.Count > 0)
                .ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value?.Errors.Select(e => e.ErrorMessage).ToArray()
                );
            return BadRequest(new { 
                message = "数据验证失败", 
                errors = errors
            });
        }

        // 检查患者是否存在
        var patient = await _context.Patients.FindAsync(dto.PatientId);
        if (patient == null)
        {
            return NotFound(new { message = "患者不存在" });
        }

        // 解析日期字符串
        DateTime? startDate = null;
        if (!string.IsNullOrEmpty(dto.StartDate))
        {
            if (DateTime.TryParse(dto.StartDate, out var parsedStartDate))
            {
                startDate = DateTime.SpecifyKind(parsedStartDate, DateTimeKind.Utc);
            }
        }

        DateTime? endDate = null;
        if (!string.IsNullOrEmpty(dto.EndDate))
        {
            if (DateTime.TryParse(dto.EndDate, out var parsedEndDate))
            {
                endDate = DateTime.SpecifyKind(parsedEndDate, DateTimeKind.Utc);
            }
        }

        DateTime? scheduledTime = null;
        if (!string.IsNullOrEmpty(dto.ScheduledTime))
        {
            if (DateTime.TryParse(dto.ScheduledTime, out var parsedScheduledTime))
            {
                scheduledTime = DateTime.SpecifyKind(parsedScheduledTime, DateTimeKind.Utc);
            }
        }

        // 生成唯一的医嘱号（如果未提供）
        string orderNumber = dto.OrderNumber ?? string.Empty;
        if (string.IsNullOrEmpty(orderNumber))
        {
            // 生成格式：ORD + 日期时间 + 随机数
            orderNumber = $"ORD{DateTime.UtcNow:yyyyMMddHHmmss}{new Random().Next(1000, 9999)}";
            
            // 确保唯一性
            while (await _context.MedicationOrders.AnyAsync(o => o.OrderNumber == orderNumber))
            {
                orderNumber = $"ORD{DateTime.UtcNow:yyyyMMddHHmmss}{new Random().Next(1000, 9999)}";
            }
        }
        else
        {
            // 如果提供了OrderNumber，检查是否已存在
            var existingOrder = await _context.MedicationOrders
                .FirstOrDefaultAsync(o => o.OrderNumber == orderNumber);
            if (existingOrder != null)
            {
                return Conflict(new { message = $"医嘱号 {orderNumber} 已存在" });
            }
        }

        // 创建医嘱实体
        var order = new MedicationOrder
        {
            Id = Guid.NewGuid(),
            PatientId = dto.PatientId,
            OrderNumber = orderNumber,
            MedicationName = dto.MedicationName,
            Specification = dto.Specification,
            Quantity = dto.Quantity,
            Unit = dto.Unit,
            Dosage = string.Empty, // 保留字段，兼容旧数据
            Frequency = dto.Frequency,
            Route = dto.Route,
            OrderType = dto.OrderType,
            StartDate = startDate,
            EndDate = endDate,
            ScheduledTime = scheduledTime,
            DoctorName = dto.DoctorName,
            Notes = dto.Notes,
            OrderDate = DateTime.UtcNow,
            Status = "待执行",
            CreatedAt = DateTime.UtcNow,
            CreatedBy = User.Identity?.Name,
        };

        // 检查药物过敏
        if (!string.IsNullOrEmpty(patient.Allergies))
        {
            var allergies = EncryptionHelper.Decrypt(patient.Allergies);
            if (allergies.Contains(order.MedicationName, StringComparison.OrdinalIgnoreCase))
            {
                order.HasAllergyWarning = true;
            }
        }

        // 检查配伍禁忌（这里简化处理，实际应该查询药物配伍禁忌数据库）
        order.HasContraindication = await CheckContraindication(order);

        _context.MedicationOrders.Add(order);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetOrdersByPatient), new { patientId = order.PatientId }, order);
    }

    /// <summary>
    /// 执行医嘱
    /// </summary>
    [HttpPost("{orderId}/execute")]
    [Authorize(Roles = "Admin,Nurse,HeadNurse")]
    [ProducesResponseType(typeof(MedicationExecution), StatusCodes.Status201Created)]
    public async Task<IActionResult> ExecuteOrder(Guid orderId, [FromBody] ExecuteMedicationOrderDto dto)
    {
        if (dto == null)
        {
            dto = new ExecuteMedicationOrderDto();
        }

        var order = await _context.MedicationOrders
            .Include(o => o.Patient)
            .FirstOrDefaultAsync(o => o.Id == orderId);

        if (order == null)
        {
            return NotFound(new { message = "医嘱不存在" });
        }

        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            _logger.LogWarning("执行医嘱失败：无法从JWT token中获取用户ID");
            return Unauthorized(new { message = "无法获取用户ID，请重新登录" });
        }

        // 验证用户是否存在且有效
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == userId);
        
        if (user == null)
        {
            _logger.LogWarning("执行医嘱失败：用户ID {UserId} 在数据库中不存在", userId);
            return BadRequest(new { message = "用户不存在，请重新登录" });
        }

        if (user.IsDeleted)
        {
            _logger.LogWarning("执行医嘱失败：用户ID {UserId} 已被删除", userId);
            return BadRequest(new { message = "用户已被删除，请重新登录" });
        }

        if (!user.IsActive)
        {
            _logger.LogWarning("执行医嘱失败：用户ID {UserId} 已被禁用", userId);
            return BadRequest(new { message = "用户已被禁用，请联系管理员" });
        }

        // 创建执行记录实体
        var execution = new MedicationExecution
        {
            Id = Guid.NewGuid(),
            MedicationOrderId = orderId,
            ExecutedByUserId = userId,
            ScheduledTime = order.ScheduledTime ?? order.StartDate ?? DateTime.UtcNow,
            ExecutedTime = DateTime.UtcNow,
            Status = "已执行",
            IsVerified = !string.IsNullOrEmpty(dto.VerificationCode),
            VerificationCode = dto.VerificationCode,
            ElectronicSignature = User.Identity?.Name,
            Notes = dto.Notes,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = User.Identity?.Name,
        };

        _context.MedicationExecutions.Add(execution);

        // 更新医嘱状态
        var hasPendingExecutions = await _context.MedicationExecutions
            .AnyAsync(e => e.MedicationOrderId == orderId && e.Status == "待执行");

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

        // 加载导航属性以便返回
        await _context.Entry(execution).Reference(e => e.ExecutedBy).LoadAsync();

        return CreatedAtAction(nameof(ExecuteOrder), new { orderId }, execution);
    }

    /// <summary>
    /// 扫码核对患者身份
    /// </summary>
    [HttpPost("verify-patient")]
    [Authorize(Roles = "Admin,Nurse,HeadNurse")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> VerifyPatient([FromBody] VerifyPatientDto dto)
    {
        var patient = await _context.Patients.FindAsync(dto.PatientId);
        if (patient == null)
        {
            return NotFound(new { message = "患者不存在" });
        }

        // 验证二维码（这里简化处理，实际应该验证二维码内容）
        var isValid = patient.AdmissionNumber == dto.VerificationCode;

        return Ok(new
        {
            isValid,
            patient = new
            {
                patient.Id,
                patient.Name,
                patient.AdmissionNumber,
                patient.Diagnosis
            }
        });
    }

    /// <summary>
    /// 检查配伍禁忌
    /// </summary>
    private async Task<bool> CheckContraindication(MedicationOrder order)
    {
        // 这里应该查询药物配伍禁忌数据库
        // 简化处理：检查是否有其他正在执行的医嘱
        var activeOrders = await _context.MedicationOrders
            .Where(o => o.PatientId == order.PatientId &&
                       o.Status != "已完成" &&
                       o.Status != "已停止" &&
                       o.Id != order.Id)
            .ToListAsync();

        // 实际应该检查药物配伍禁忌表
        return false; // 简化处理
    }

    /// <summary>
    /// 更新医嘱
    /// </summary>
    [HttpPut("{id}")]
    [Authorize(Roles = "Admin,Doctor,HeadNurse")]
    [ProducesResponseType(typeof(MedicationOrder), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateOrder(Guid id, [FromBody] CreateMedicationOrderDto dto)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState
                .Where(x => x.Value?.Errors.Count > 0)
                .ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value?.Errors.Select(e => e.ErrorMessage).ToArray()
                );
            return BadRequest(new { 
                message = "数据验证失败", 
                errors = errors
            });
        }

        var order = await _context.MedicationOrders.FindAsync(id);
        if (order == null)
        {
            return NotFound(new { message = "医嘱不存在" });
        }

        // 解析日期字符串
        DateTime? startDate = null;
        if (!string.IsNullOrEmpty(dto.StartDate))
        {
            if (DateTime.TryParse(dto.StartDate, out var parsedStartDate))
            {
                startDate = DateTime.SpecifyKind(parsedStartDate, DateTimeKind.Utc);
            }
        }

        DateTime? endDate = null;
        if (!string.IsNullOrEmpty(dto.EndDate))
        {
            if (DateTime.TryParse(dto.EndDate, out var parsedEndDate))
            {
                endDate = DateTime.SpecifyKind(parsedEndDate, DateTimeKind.Utc);
            }
        }

        DateTime? scheduledTime = null;
        if (!string.IsNullOrEmpty(dto.ScheduledTime))
        {
            if (DateTime.TryParse(dto.ScheduledTime, out var parsedScheduledTime))
            {
                scheduledTime = DateTime.SpecifyKind(parsedScheduledTime, DateTimeKind.Utc);
            }
        }

        // 更新字段
        order.MedicationName = dto.MedicationName;
        order.Specification = dto.Specification;
        order.Quantity = dto.Quantity;
        order.Unit = dto.Unit;
        order.Frequency = dto.Frequency;
        order.Route = dto.Route;
        order.OrderType = dto.OrderType;
        order.StartDate = startDate;
        order.EndDate = endDate;
        order.ScheduledTime = scheduledTime;
        order.DoctorName = dto.DoctorName;
        order.Notes = dto.Notes;
        order.UpdatedAt = DateTime.UtcNow;
        order.UpdatedBy = User.Identity?.Name;

        await _context.SaveChangesAsync();

        return Ok(order);
    }

    /// <summary>
    /// 删除医嘱
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin,Doctor,HeadNurse")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteOrder(Guid id)
    {
        var order = await _context.MedicationOrders.FindAsync(id);
        if (order == null)
        {
            return NotFound(new { message = "医嘱不存在" });
        }

        _context.MedicationOrders.Remove(order);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    /// <summary>
    /// 停止医嘱
    /// </summary>
    [HttpPut("{id}/stop")]
    [Authorize(Roles = "Admin,Doctor,HeadNurse")]
    [ProducesResponseType(typeof(MedicationOrder), StatusCodes.Status200OK)]
    public async Task<IActionResult> StopOrder(Guid id, [FromBody] StopOrderDto dto)
    {
        var order = await _context.MedicationOrders.FindAsync(id);
        if (order == null)
        {
            return NotFound(new { message = "医嘱不存在" });
        }

        order.Status = "已停止";
        order.EndDate = DateTime.UtcNow;
        order.Notes = string.IsNullOrEmpty(order.Notes) 
            ? $"停止原因：{dto.StopReason ?? "无"}" 
            : $"{order.Notes}\n停止原因：{dto.StopReason ?? "无"}";
        order.UpdatedAt = DateTime.UtcNow;
        order.UpdatedBy = User.Identity?.Name;

        await _context.SaveChangesAsync();

        return Ok(order);
    }

    /// <summary>
    /// 获取医嘱执行记录
    /// </summary>
    [HttpGet("{orderId}/executions")]
    [ProducesResponseType(typeof(List<MedicationExecution>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetExecutions(Guid orderId)
    {
        var executions = await _context.MedicationExecutions
            .Include(e => e.ExecutedBy)
            .Where(e => e.MedicationOrderId == orderId)
            .OrderByDescending(e => e.ExecutedTime)
            .ToListAsync();

        return Ok(executions);
    }
}

/// <summary>
/// 患者验证DTO
/// </summary>
public class VerifyPatientDto
{
    public Guid PatientId { get; set; }
    public string VerificationCode { get; set; } = string.Empty; // 二维码内容
}

/// <summary>
/// 停止医嘱DTO
/// </summary>
public class StopOrderDto
{
    public string? StopReason { get; set; }
}

