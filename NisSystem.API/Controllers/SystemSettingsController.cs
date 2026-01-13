using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NisSystem.API.Data;
using NisSystem.API.DTOs;
using NisSystem.API.Models;

namespace NisSystem.API.Controllers;

/// <summary>
/// 系统设置控制器
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SystemSettingsController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<SystemSettingsController> _logger;

    public SystemSettingsController(ApplicationDbContext context, ILogger<SystemSettingsController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// 获取系统设置（基础信息）
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(SystemSettingsDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSystemSettings()
    {
        var settings = await _context.SystemSettings
            .Where(s => s.IsActive)
            .OrderByDescending(s => s.CreatedAt)
            .FirstOrDefaultAsync();

        if (settings == null)
        {
            // 返回默认值
            return Ok(new SystemSettingsDto
            {
                Id = Guid.Empty,
                HospitalName = string.Empty,
                HospitalCode = string.Empty,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
            });
        }

        var dto = new SystemSettingsDto
        {
            Id = settings.Id,
            HospitalName = settings.HospitalName,
            HospitalCode = settings.HospitalCode,
            Address = settings.Address,
            Phone = settings.Phone,
            Email = settings.Email,
            Website = settings.Website,
            Description = settings.Description,
            IsActive = settings.IsActive,
            CreatedAt = settings.CreatedAt,
            UpdatedAt = settings.UpdatedAt,
        };

        return Ok(dto);
    }

    /// <summary>
    /// 创建或更新系统设置
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(SystemSettingsDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> CreateOrUpdateSystemSettings([FromBody] CreateOrUpdateSystemSettingsDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        // 查找现有的设置
        var existingSettings = await _context.SystemSettings
            .Where(s => s.IsActive)
            .OrderByDescending(s => s.CreatedAt)
            .FirstOrDefaultAsync();

        SystemSettings settings;

        if (existingSettings != null)
        {
            // 更新现有设置
            settings = existingSettings;
            settings.HospitalName = dto.HospitalName;
            settings.HospitalCode = dto.HospitalCode;
            settings.Address = dto.Address;
            settings.Phone = dto.Phone;
            settings.Email = dto.Email;
            settings.Website = dto.Website;
            settings.Description = dto.Description;
            settings.UpdatedAt = DateTime.UtcNow;
            settings.UpdatedBy = User.Identity?.Name;
        }
        else
        {
            // 创建新设置
            settings = new SystemSettings
            {
                Id = Guid.NewGuid(),
                HospitalName = dto.HospitalName,
                HospitalCode = dto.HospitalCode,
                Address = dto.Address,
                Phone = dto.Phone,
                Email = dto.Email,
                Website = dto.Website,
                Description = dto.Description,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = User.Identity?.Name,
            };
            _context.SystemSettings.Add(settings);
        }

        await _context.SaveChangesAsync();

        var result = new SystemSettingsDto
        {
            Id = settings.Id,
            HospitalName = settings.HospitalName,
            HospitalCode = settings.HospitalCode,
            Address = settings.Address,
            Phone = settings.Phone,
            Email = settings.Email,
            Website = settings.Website,
            Description = settings.Description,
            IsActive = settings.IsActive,
            CreatedAt = settings.CreatedAt,
            UpdatedAt = settings.UpdatedAt,
        };

        return Ok(result);
    }
}

