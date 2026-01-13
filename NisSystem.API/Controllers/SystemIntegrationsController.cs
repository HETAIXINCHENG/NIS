using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NisSystem.API.Data;
using NisSystem.API.DTOs;
using NisSystem.API.Models;
using System.Text.Json;

namespace NisSystem.API.Controllers;

/// <summary>
/// 系统集成控制器
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SystemIntegrationsController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<SystemIntegrationsController> _logger;

    public SystemIntegrationsController(ApplicationDbContext context, ILogger<SystemIntegrationsController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// 接收HIS系统患者信息
    /// </summary>
    [HttpPost("his/patient")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<IActionResult> ReceiveHisPatient([FromBody] HisPatientDto dto)
    {
        try
        {
            // 检查患者是否已存在
            var existingPatient = await _context.Patients
                .FirstOrDefaultAsync(p => p.AdmissionNumber == dto.AdmissionNumber);

            Patient patient;
            if (existingPatient != null)
            {
                // 更新患者信息
                existingPatient.Name = dto.Name;
                existingPatient.Gender = dto.Gender;
                existingPatient.Age = dto.Age;
                existingPatient.Diagnosis = !string.IsNullOrEmpty(dto.Diagnosis) 
                    ? Helpers.EncryptionHelper.Encrypt(dto.Diagnosis) 
                    : existingPatient.Diagnosis;
                existingPatient.Department = dto.Department;
                existingPatient.RoomNumber = dto.RoomNumber;
                existingPatient.BedNumber = dto.BedNumber;
                existingPatient.UpdatedAt = DateTime.UtcNow;
                patient = existingPatient;
            }
            else
            {
                // 创建新患者
                patient = new Patient
                {
                    Id = Guid.NewGuid(),
                    Name = dto.Name,
                    Gender = dto.Gender,
                    Age = dto.Age,
                    AdmissionNumber = dto.AdmissionNumber,
                    Diagnosis = !string.IsNullOrEmpty(dto.Diagnosis) 
                        ? Helpers.EncryptionHelper.Encrypt(dto.Diagnosis) 
                        : null,
                    Department = dto.Department,
                    RoomNumber = dto.RoomNumber,
                    BedNumber = dto.BedNumber,
                    Status = "在院",
                    AdmissionDate = DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = "HIS系统"
                };
                _context.Patients.Add(patient);
            }

            await _context.SaveChangesAsync();

            // 记录集成日志
            var integration = new SystemIntegration
            {
                Id = Guid.NewGuid(),
                SystemName = "HIS",
                IntegrationType = "患者信息",
                RequestData = JsonSerializer.Serialize(dto),
                ResponseData = JsonSerializer.Serialize(new { PatientId = patient.Id, Status = "成功" }),
                Status = "成功",
                IntegratedAt = DateTime.UtcNow,
                RelatedPatientId = patient.Id,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = "HIS系统"
            };

            _context.SystemIntegrations.Add(integration);
            await _context.SaveChangesAsync();

            return Ok(new { PatientId = patient.Id, Message = "患者信息同步成功" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "接收HIS患者信息失败");
            
            // 记录失败日志
            var integration = new SystemIntegration
            {
                Id = Guid.NewGuid(),
                SystemName = "HIS",
                IntegrationType = "患者信息",
                RequestData = JsonSerializer.Serialize(dto),
                Status = "失败",
                ErrorMessage = ex.Message,
                IntegratedAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = "HIS系统"
            };

            _context.SystemIntegrations.Add(integration);
            await _context.SaveChangesAsync();

            return StatusCode(500, new { Message = "同步失败", Error = ex.Message });
        }
    }

    /// <summary>
    /// 接收HIS系统医嘱信息
    /// </summary>
    [HttpPost("his/order")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<IActionResult> ReceiveHisOrder([FromBody] HisOrderDto dto)
    {
        try
        {
            var patient = await _context.Patients
                .FirstOrDefaultAsync(p => p.AdmissionNumber == dto.AdmissionNumber);

            if (patient == null)
            {
                return NotFound(new { Message = "患者不存在" });
            }

            // 检查医嘱号是否已存在，如果存在则更新，否则创建
            MedicationOrder order;
            var existingOrder = await _context.MedicationOrders
                .FirstOrDefaultAsync(o => o.OrderNumber == dto.OrderNumber);

            if (existingOrder != null)
            {
                // 更新现有医嘱
                order = existingOrder;
                order.PatientId = patient.Id;
                order.MedicationName = dto.MedicationName;
                order.Dosage = dto.Dosage;
                order.Frequency = dto.Frequency;
                order.Route = dto.Route;
                order.OrderType = dto.OrderType;
                order.StartDate = dto.StartDate;
                order.EndDate = dto.EndDate;
                order.DoctorName = dto.DoctorName;
                order.ScheduledTime = dto.ScheduledTime;
                order.UpdatedAt = DateTime.UtcNow;
                order.UpdatedBy = "HIS系统";
            }
            else
            {
                // 创建新医嘱
                order = new MedicationOrder
                {
                    Id = Guid.NewGuid(),
                    PatientId = patient.Id,
                    OrderNumber = dto.OrderNumber,
                    MedicationName = dto.MedicationName,
                    Dosage = dto.Dosage,
                    Frequency = dto.Frequency,
                    Route = dto.Route,
                    OrderType = dto.OrderType,
                    OrderDate = DateTime.UtcNow,
                    StartDate = dto.StartDate,
                    EndDate = dto.EndDate,
                    Status = "待执行",
                    DoctorName = dto.DoctorName,
                    ScheduledTime = dto.ScheduledTime,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = "HIS系统"
                };
                _context.MedicationOrders.Add(order);
            }

            await _context.SaveChangesAsync();

            // 记录集成日志
            var integration = new SystemIntegration
            {
                Id = Guid.NewGuid(),
                SystemName = "HIS",
                IntegrationType = "医嘱信息",
                RequestData = JsonSerializer.Serialize(dto),
                ResponseData = JsonSerializer.Serialize(new { OrderId = order.Id, Status = "成功" }),
                Status = "成功",
                IntegratedAt = DateTime.UtcNow,
                RelatedPatientId = patient.Id,
                RelatedOrderId = order.Id,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = "HIS系统"
            };

            _context.SystemIntegrations.Add(integration);
            await _context.SaveChangesAsync();

            return Ok(new { OrderId = order.Id, Message = "医嘱同步成功" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "接收HIS医嘱信息失败");
            return StatusCode(500, new { Message = "同步失败", Error = ex.Message });
        }
    }

    /// <summary>
    /// 获取系统集成日志
    /// </summary>
    [HttpGet("logs")]
    [ProducesResponseType(typeof(List<SystemIntegrationDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetIntegrationLogs(
        [FromQuery] string? systemName,
        [FromQuery] string? status,
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate)
    {
        var query = _context.SystemIntegrations.AsQueryable();

        if (!string.IsNullOrEmpty(systemName))
        {
            query = query.Where(i => i.SystemName == systemName);
        }

        if (!string.IsNullOrEmpty(status))
        {
            query = query.Where(i => i.Status == status);
        }

        if (startDate.HasValue)
        {
            var startDateUtc = DateTime.SpecifyKind(startDate.Value, DateTimeKind.Utc);
            query = query.Where(i => i.IntegratedAt >= startDateUtc);
        }

        if (endDate.HasValue)
        {
            var endDateUtc = DateTime.SpecifyKind(endDate.Value, DateTimeKind.Utc);
            query = query.Where(i => i.IntegratedAt <= endDateUtc);
        }

        var logs = await query
            .OrderByDescending(i => i.IntegratedAt)
            .Take(100)
            .ToListAsync();

        var dtos = logs.Select(i => new SystemIntegrationDto
        {
            Id = i.Id,
            SystemName = i.SystemName,
            IntegrationType = i.IntegrationType,
            RequestData = i.RequestData,
            ResponseData = i.ResponseData,
            Status = i.Status,
            IntegratedAt = i.IntegratedAt,
            ErrorMessage = i.ErrorMessage,
            RelatedPatientId = i.RelatedPatientId,
            RelatedOrderId = i.RelatedOrderId,
            CreatedAt = i.CreatedAt,
            UpdatedAt = i.UpdatedAt,
        }).ToList();

        return Ok(dtos);
    }

    /// <summary>
    /// 获取所有系统集成记录（分页）
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<SystemIntegrationDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSystemIntegrations(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? systemName = null,
        [FromQuery] string? status = null,
        [FromQuery] string? integrationType = null)
    {
        var query = _context.SystemIntegrations.AsQueryable();

        if (!string.IsNullOrEmpty(systemName))
        {
            query = query.Where(i => i.SystemName.Contains(systemName));
        }

        if (!string.IsNullOrEmpty(status))
        {
            query = query.Where(i => i.Status == status);
        }

        if (!string.IsNullOrEmpty(integrationType))
        {
            query = query.Where(i => i.IntegrationType.Contains(integrationType));
        }

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(i => i.IntegratedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var dtos = items.Select(i => new SystemIntegrationDto
        {
            Id = i.Id,
            SystemName = i.SystemName,
            IntegrationType = i.IntegrationType,
            RequestData = i.RequestData,
            ResponseData = i.ResponseData,
            Status = i.Status,
            IntegratedAt = i.IntegratedAt,
            ErrorMessage = i.ErrorMessage,
            RelatedPatientId = i.RelatedPatientId,
            RelatedOrderId = i.RelatedOrderId,
            CreatedAt = i.CreatedAt,
            UpdatedAt = i.UpdatedAt,
        }).ToList();

        return Ok(new PagedResult<SystemIntegrationDto>
        {
            Items = dtos,
            TotalCount = total,
            Page = page,
            PageSize = pageSize,
            TotalPages = (int)Math.Ceiling(total / (double)pageSize)
        });
    }

    /// <summary>
    /// 根据ID获取系统集成记录
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(SystemIntegrationDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSystemIntegration(Guid id)
    {
        var integration = await _context.SystemIntegrations.FindAsync(id);
        if (integration == null)
        {
            return NotFound(new { Message = "系统集成记录不存在" });
        }

        var dto = new SystemIntegrationDto
        {
            Id = integration.Id,
            SystemName = integration.SystemName,
            IntegrationType = integration.IntegrationType,
            RequestData = integration.RequestData,
            ResponseData = integration.ResponseData,
            Status = integration.Status,
            IntegratedAt = integration.IntegratedAt,
            ErrorMessage = integration.ErrorMessage,
            RelatedPatientId = integration.RelatedPatientId,
            RelatedOrderId = integration.RelatedOrderId,
            CreatedAt = integration.CreatedAt,
            UpdatedAt = integration.UpdatedAt,
        };

        return Ok(dto);
    }

    /// <summary>
    /// 创建系统集成记录
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(SystemIntegrationDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateSystemIntegration([FromBody] CreateSystemIntegrationDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var integration = new SystemIntegration
        {
            Id = Guid.NewGuid(),
            SystemName = dto.SystemName,
            IntegrationType = dto.IntegrationType,
            RequestData = dto.RequestData,
            ResponseData = dto.ResponseData,
            Status = dto.Status,
            IntegratedAt = dto.IntegratedAt ?? DateTime.UtcNow,
            ErrorMessage = dto.ErrorMessage,
            RelatedPatientId = dto.RelatedPatientId,
            RelatedOrderId = dto.RelatedOrderId,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = User.Identity?.Name,
        };

        _context.SystemIntegrations.Add(integration);
        await _context.SaveChangesAsync();

        var result = new SystemIntegrationDto
        {
            Id = integration.Id,
            SystemName = integration.SystemName,
            IntegrationType = integration.IntegrationType,
            RequestData = integration.RequestData,
            ResponseData = integration.ResponseData,
            Status = integration.Status,
            IntegratedAt = integration.IntegratedAt,
            ErrorMessage = integration.ErrorMessage,
            RelatedPatientId = integration.RelatedPatientId,
            RelatedOrderId = integration.RelatedOrderId,
            CreatedAt = integration.CreatedAt,
            UpdatedAt = integration.UpdatedAt,
        };

        return CreatedAtAction(nameof(GetSystemIntegration), new { id = integration.Id }, result);
    }

    /// <summary>
    /// 更新系统集成记录
    /// </summary>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(SystemIntegrationDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateSystemIntegration(Guid id, [FromBody] UpdateSystemIntegrationDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var integration = await _context.SystemIntegrations.FindAsync(id);
        if (integration == null)
        {
            return NotFound(new { Message = "系统集成记录不存在" });
        }

        if (!string.IsNullOrEmpty(dto.SystemName))
        {
            integration.SystemName = dto.SystemName;
        }

        if (!string.IsNullOrEmpty(dto.IntegrationType))
        {
            integration.IntegrationType = dto.IntegrationType;
        }

        if (!string.IsNullOrEmpty(dto.RequestData))
        {
            integration.RequestData = dto.RequestData;
        }

        if (dto.ResponseData != null)
        {
            integration.ResponseData = dto.ResponseData;
        }

        if (!string.IsNullOrEmpty(dto.Status))
        {
            integration.Status = dto.Status;
        }

        if (dto.IntegratedAt.HasValue)
        {
            integration.IntegratedAt = DateTime.SpecifyKind(dto.IntegratedAt.Value, DateTimeKind.Utc);
        }

        if (dto.ErrorMessage != null)
        {
            integration.ErrorMessage = dto.ErrorMessage;
        }

        if (dto.RelatedPatientId.HasValue)
        {
            integration.RelatedPatientId = dto.RelatedPatientId;
        }

        if (dto.RelatedOrderId.HasValue)
        {
            integration.RelatedOrderId = dto.RelatedOrderId;
        }

        integration.UpdatedAt = DateTime.UtcNow;
        integration.UpdatedBy = User.Identity?.Name;

        await _context.SaveChangesAsync();

        var result = new SystemIntegrationDto
        {
            Id = integration.Id,
            SystemName = integration.SystemName,
            IntegrationType = integration.IntegrationType,
            RequestData = integration.RequestData,
            ResponseData = integration.ResponseData,
            Status = integration.Status,
            IntegratedAt = integration.IntegratedAt,
            ErrorMessage = integration.ErrorMessage,
            RelatedPatientId = integration.RelatedPatientId,
            RelatedOrderId = integration.RelatedOrderId,
            CreatedAt = integration.CreatedAt,
            UpdatedAt = integration.UpdatedAt,
        };

        return Ok(result);
    }

    /// <summary>
    /// 删除系统集成记录
    /// </summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteSystemIntegration(Guid id)
    {
        var integration = await _context.SystemIntegrations.FindAsync(id);
        if (integration == null)
        {
            return NotFound(new { Message = "系统集成记录不存在" });
        }

        _context.SystemIntegrations.Remove(integration);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}


/// <summary>
/// HIS患者信息DTO
/// </summary>
public class HisPatientDto
{
    public string AdmissionNumber { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Gender { get; set; } = string.Empty;
    public int Age { get; set; }
    public string? Diagnosis { get; set; }
    public string? Department { get; set; }
    public string? RoomNumber { get; set; }
    public string? BedNumber { get; set; }
}

/// <summary>
/// HIS医嘱信息DTO
/// </summary>
public class HisOrderDto
{
    public string AdmissionNumber { get; set; } = string.Empty;
    public string OrderNumber { get; set; } = string.Empty;
    public string MedicationName { get; set; } = string.Empty;
    public string Dosage { get; set; } = string.Empty;
    public string Frequency { get; set; } = string.Empty;
    public string Route { get; set; } = string.Empty;
    public string OrderType { get; set; } = "长期";
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? DoctorName { get; set; }
    public DateTime? ScheduledTime { get; set; }
}
