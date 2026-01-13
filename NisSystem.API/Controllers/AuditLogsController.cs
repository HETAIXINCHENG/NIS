using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NisSystem.API.Data;
using NisSystem.API.DTOs;
using NisSystem.API.Models;

namespace NisSystem.API.Controllers;

/// <summary>
/// 审计日志控制器
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AuditLogsController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<AuditLogsController> _logger;

    public AuditLogsController(ApplicationDbContext context, ILogger<AuditLogsController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// 获取审计日志列表
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "Admin,HeadNurse,Doctor")]
    [ProducesResponseType(typeof(PagedResult<AuditLog>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAuditLogs(
        [FromQuery] Guid? userId,
        [FromQuery] string? action,
        [FromQuery] string? entityType,
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        var query = _context.AuditLogs
            .Include(a => a.User)
            .AsQueryable();

        if (userId.HasValue)
        {
            query = query.Where(a => a.UserId == userId.Value);
        }

        if (!string.IsNullOrEmpty(action))
        {
            query = query.Where(a => a.Action == action);
        }

        if (!string.IsNullOrEmpty(entityType))
        {
            query = query.Where(a => a.EntityType == entityType);
        }

        if (startDate.HasValue)
        {
            query = query.Where(a => a.CreatedAt >= startDate.Value);
        }

        if (endDate.HasValue)
        {
            query = query.Where(a => a.CreatedAt <= endDate.Value);
        }

        var totalCount = await query.CountAsync();
        var logs = await query
            .OrderByDescending(a => a.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var result = new PagedResult<AuditLog>
        {
            Items = logs,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
        };

        return Ok(result);
    }

    /// <summary>
    /// 获取用户操作日志
    /// </summary>
    [HttpGet("user/{userId}")]
    [Authorize(Roles = "Admin,HeadNurse,Doctor")]
    [ProducesResponseType(typeof(List<AuditLog>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUserAuditLogs(
        Guid userId,
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate)
    {
        var query = _context.AuditLogs
            .Include(a => a.User)
            .Where(a => a.UserId == userId)
            .AsQueryable();

        if (startDate.HasValue)
        {
            query = query.Where(a => a.CreatedAt >= startDate.Value);
        }

        if (endDate.HasValue)
        {
            query = query.Where(a => a.CreatedAt <= endDate.Value);
        }

        var logs = await query
            .OrderByDescending(a => a.CreatedAt)
            .Take(100)
            .ToListAsync();

        return Ok(logs);
    }
}
