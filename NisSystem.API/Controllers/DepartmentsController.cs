using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NisSystem.API.Data;
using NisSystem.API.DTOs;
using NisSystem.API.Models;
using System.Security.Claims;

namespace NisSystem.API.Controllers;

/// <summary>
/// 科室管理控制器
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DepartmentsController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<DepartmentsController> _logger;

    public DepartmentsController(ApplicationDbContext context, ILogger<DepartmentsController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// 获取科室列表
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<Department>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDepartments([FromQuery] string? type, [FromQuery] bool? isActive)
    {
        var query = _context.Departments.AsQueryable();

        if (!string.IsNullOrEmpty(type))
        {
            query = query.Where(d => d.Type == type);
        }

        if (isActive.HasValue)
        {
            query = query.Where(d => d.IsActive == isActive.Value);
        }

        var departments = await query
            .OrderBy(d => d.Name)
            .ToListAsync();

        return Ok(departments);
    }

    /// <summary>
    /// 获取单个科室
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(Department), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDepartment(Guid id)
    {
        var department = await _context.Departments.FindAsync(id);

        if (department == null)
        {
            return NotFound(new { message = "科室不存在" });
        }

        return Ok(department);
    }

    /// <summary>
    /// 创建科室
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Admin,HeadNurse")]
    [ProducesResponseType(typeof(Department), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateDepartment([FromBody] CreateDepartmentDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        // 检查科室名称是否已存在
        if (await _context.Departments.AnyAsync(d => d.Name == dto.Name))
        {
            return BadRequest(new { message = "科室名称已存在" });
        }

        // 检查科室代码是否已存在
        if (!string.IsNullOrEmpty(dto.Code) && await _context.Departments.AnyAsync(d => d.Code == dto.Code))
        {
            return BadRequest(new { message = "科室代码已存在" });
        }

        var department = new Department
        {
            Id = Guid.NewGuid(),
            Name = dto.Name,
            Code = dto.Code,
            Description = dto.Description,
            Type = dto.Type,
            Director = dto.Director,
            Phone = dto.Phone,
            Location = dto.Location,
            IsActive = dto.IsActive ?? true,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = User.Identity?.Name,
        };

        _context.Departments.Add(department);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetDepartment), new { id = department.Id }, department);
    }

    /// <summary>
    /// 更新科室
    /// </summary>
    [HttpPut("{id}")]
    [Authorize(Roles = "Admin,HeadNurse")]
    [ProducesResponseType(typeof(Department), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateDepartment(Guid id, [FromBody] UpdateDepartmentDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var department = await _context.Departments.FindAsync(id);

        if (department == null)
        {
            return NotFound(new { message = "科室不存在" });
        }

        // 检查科室名称是否已被其他科室使用
        if (!string.IsNullOrEmpty(dto.Name) && dto.Name != department.Name)
        {
            if (await _context.Departments.AnyAsync(d => d.Name == dto.Name && d.Id != id))
            {
                return BadRequest(new { message = "科室名称已存在" });
            }
            department.Name = dto.Name;
        }

        // 检查科室代码是否已被其他科室使用
        if (!string.IsNullOrEmpty(dto.Code) && dto.Code != department.Code)
        {
            if (await _context.Departments.AnyAsync(d => d.Code == dto.Code && d.Id != id))
            {
                return BadRequest(new { message = "科室代码已存在" });
            }
            department.Code = dto.Code;
        }

        if (dto.Description != null)
        {
            department.Description = dto.Description;
        }

        if (dto.Type != null)
        {
            department.Type = dto.Type;
        }

        if (dto.Director != null)
        {
            department.Director = dto.Director;
        }

        if (dto.Phone != null)
        {
            department.Phone = dto.Phone;
        }

        if (dto.Location != null)
        {
            department.Location = dto.Location;
        }

        if (dto.IsActive.HasValue)
        {
            department.IsActive = dto.IsActive.Value;
        }

        department.UpdatedAt = DateTime.UtcNow;
        department.UpdatedBy = User.Identity?.Name;

        await _context.SaveChangesAsync();

        return Ok(department);
    }

    /// <summary>
    /// 删除科室（软删除）
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteDepartment(Guid id)
    {
        var department = await _context.Departments.FindAsync(id);

        if (department == null)
        {
            return NotFound(new { message = "科室不存在" });
        }

        // 检查是否有用户或患者使用该科室
        var hasUsers = await _context.Users.AnyAsync(u => u.Department == department.Name && !u.IsDeleted);
        var hasPatients = await _context.Patients.AnyAsync(p => p.Department == department.Name && !p.IsDeleted);

        if (hasUsers || hasPatients)
        {
            return BadRequest(new { message = "该科室正在使用中，无法删除" });
        }

        department.IsDeleted = true;
        department.UpdatedAt = DateTime.UtcNow;
        department.UpdatedBy = User.Identity?.Name;

        await _context.SaveChangesAsync();

        return NoContent();
    }
}

