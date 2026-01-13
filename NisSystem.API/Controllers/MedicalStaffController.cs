using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NisSystem.API.Data;
using NisSystem.API.DTOs;
using NisSystem.API.Models;
using System.Security.Claims;

namespace NisSystem.API.Controllers;

/// <summary>
/// 医护信息管理控制器
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class MedicalStaffController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<MedicalStaffController> _logger;

    public MedicalStaffController(ApplicationDbContext context, ILogger<MedicalStaffController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// 获取医护信息列表（支持搜索、分页、筛选）
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<MedicalStaffDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMedicalStaff(
        [FromQuery] string? search,
        [FromQuery] Guid? departmentId,
        [FromQuery] string? position,
        [FromQuery] bool? isActive,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        try
        {
            var query = _context.Users
                .Include(u => u.Role)
                .Include(u => u.DepartmentNavigation)
                .AsQueryable();

            // 只获取护士、护师、护长等医护角色
            query = query.Where(u => 
                u.Position == "护士" || 
                u.Position == "护师" || 
                u.Position == "护长" ||
                u.Position == "主管护师" ||
                u.Position == "副主任护师" ||
                u.Position == "主任护师");

            // 搜索功能
            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(u =>
                    u.Name.Contains(search) ||
                    u.EmployeeId.Contains(search) ||
                    (u.Phone != null && u.Phone.Contains(search)));
            }

            // 筛选
            if (departmentId.HasValue)
            {
                query = query.Where(u => u.DepartmentId == departmentId.Value);
            }

            if (!string.IsNullOrEmpty(position))
            {
                query = query.Where(u => u.Position == position);
            }

            if (isActive.HasValue)
            {
                query = query.Where(u => u.IsActive == isActive.Value);
            }

            var totalCount = await query.CountAsync();

            var users = await query
                .OrderBy(u => u.DepartmentNavigation != null ? u.DepartmentNavigation.Name : "")
                .ThenBy(u => u.Name)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var result = users.Select(u => new MedicalStaffDto
            {
                Id = u.Id,
                Username = u.Username,
                Name = u.Name,
                EmployeeId = u.EmployeeId,
                Email = u.Email,
                Phone = u.Phone,
                DepartmentId = u.DepartmentId,
                DepartmentName = u.DepartmentNavigation?.Name ?? u.Department ?? "",
                Position = u.Position,
                IsActive = u.IsActive,
                LastLoginAt = u.LastLoginAt,
                CreatedAt = u.CreatedAt,
            }).ToList();

            return Ok(new PagedResult<MedicalStaffDto>
            {
                Items = result,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取医护信息列表失败");
            return StatusCode(500, "获取医护信息列表失败");
        }
    }

    /// <summary>
    /// 获取单个医护信息
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(MedicalStaffDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMedicalStaff(Guid id)
    {
        try
        {
            var user = await _context.Users
                .Include(u => u.Role)
                .Include(u => u.DepartmentNavigation)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null)
            {
                return NotFound("医护信息不存在");
            }

            var result = new MedicalStaffDto
            {
                Id = user.Id,
                Username = user.Username,
                Name = user.Name,
                EmployeeId = user.EmployeeId,
                Email = user.Email,
                Phone = user.Phone,
                DepartmentId = user.DepartmentId,
                DepartmentName = user.DepartmentNavigation?.Name ?? user.Department ?? "",
                Position = user.Position,
                IsActive = user.IsActive,
                LastLoginAt = user.LastLoginAt,
                CreatedAt = user.CreatedAt,
            };

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取医护信息失败");
            return StatusCode(500, "获取医护信息失败");
        }
    }

    /// <summary>
    /// 创建医护信息
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Admin,HeadNurse")]
    [ProducesResponseType(typeof(MedicalStaffDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateMedicalStaff([FromBody] CreateMedicalStaffDto dto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // 检查用户名是否已存在
            if (await _context.Users.AnyAsync(u => u.Username == dto.Username))
            {
                return BadRequest("用户名已存在");
            }

            // 检查工号是否已存在
            if (await _context.Users.AnyAsync(u => u.EmployeeId == dto.EmployeeId))
            {
                return BadRequest("工号已存在");
            }

            // 获取护士角色
            var nurseRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name == "Nurse");
            if (nurseRole == null)
            {
                return BadRequest("护士角色不存在");
            }

            // 检查科室是否存在
            if (dto.DepartmentId.HasValue)
            {
                var department = await _context.Departments.FindAsync(dto.DepartmentId.Value);
                if (department == null)
                {
                    return BadRequest("科室不存在");
                }
            }

            var user = new User
            {
                Username = dto.Username,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password ?? "123456"), // 默认密码
                Name = dto.Name,
                EmployeeId = dto.EmployeeId,
                Email = dto.Email,
                Phone = dto.Phone,
                DepartmentId = dto.DepartmentId,
                Department = dto.DepartmentId.HasValue ? null : dto.DepartmentName, // 如果使用外键，清空字符串字段
                Position = dto.Position ?? "护士",
                IsActive = dto.IsActive ?? true,
                RoleId = nurseRole.Id,
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // 重新加载以包含导航属性
            await _context.Entry(user).Reference(u => u.DepartmentNavigation).LoadAsync();

            var result = new MedicalStaffDto
            {
                Id = user.Id,
                Username = user.Username,
                Name = user.Name,
                EmployeeId = user.EmployeeId,
                Email = user.Email,
                Phone = user.Phone,
                DepartmentId = user.DepartmentId,
                DepartmentName = user.DepartmentNavigation?.Name ?? user.Department ?? "",
                Position = user.Position,
                IsActive = user.IsActive,
                LastLoginAt = user.LastLoginAt,
                CreatedAt = user.CreatedAt,
            };

            return CreatedAtAction(nameof(GetMedicalStaff), new { id = user.Id }, result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "创建医护信息失败");
            return StatusCode(500, "创建医护信息失败");
        }
    }

    /// <summary>
    /// 更新医护信息
    /// </summary>
    [HttpPut("{id}")]
    [Authorize(Roles = "Admin,HeadNurse")]
    [ProducesResponseType(typeof(MedicalStaffDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateMedicalStaff(Guid id, [FromBody] UpdateMedicalStaffDto dto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var user = await _context.Users
                .Include(u => u.DepartmentNavigation)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null)
            {
                return NotFound("医护信息不存在");
            }

            // 检查用户名是否已被其他用户使用
            if (!string.IsNullOrEmpty(dto.Username) && dto.Username != user.Username)
            {
                if (await _context.Users.AnyAsync(u => u.Username == dto.Username && u.Id != id))
                {
                    return BadRequest("用户名已存在");
                }
                user.Username = dto.Username;
            }

            // 检查工号是否已被其他用户使用
            if (!string.IsNullOrEmpty(dto.EmployeeId) && dto.EmployeeId != user.EmployeeId)
            {
                if (await _context.Users.AnyAsync(u => u.EmployeeId == dto.EmployeeId && u.Id != id))
                {
                    return BadRequest("工号已存在");
                }
                user.EmployeeId = dto.EmployeeId;
            }

            // 检查科室是否存在
            if (dto.DepartmentId.HasValue)
            {
                var department = await _context.Departments.FindAsync(dto.DepartmentId.Value);
                if (department == null)
                {
                    return BadRequest("科室不存在");
                }
            }

            if (dto.Name != null) user.Name = dto.Name;
            if (dto.Email != null) user.Email = dto.Email;
            if (dto.Phone != null) user.Phone = dto.Phone;
            if (dto.DepartmentId.HasValue)
            {
                user.DepartmentId = dto.DepartmentId;
                user.Department = null; // 使用外键时清空字符串字段
            }
            else if (dto.DepartmentId == null && dto.DepartmentName != null)
            {
                user.DepartmentId = null;
                user.Department = dto.DepartmentName;
            }
            if (dto.Position != null) user.Position = dto.Position;
            if (dto.IsActive.HasValue) user.IsActive = dto.IsActive.Value;
            if (!string.IsNullOrEmpty(dto.Password))
            {
                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password);
            }

            await _context.SaveChangesAsync();

            // 重新加载以包含导航属性
            await _context.Entry(user).Reference(u => u.DepartmentNavigation).LoadAsync();

            var result = new MedicalStaffDto
            {
                Id = user.Id,
                Username = user.Username,
                Name = user.Name,
                EmployeeId = user.EmployeeId,
                Email = user.Email,
                Phone = user.Phone,
                DepartmentId = user.DepartmentId,
                DepartmentName = user.DepartmentNavigation?.Name ?? user.Department ?? "",
                Position = user.Position,
                IsActive = user.IsActive,
                LastLoginAt = user.LastLoginAt,
                CreatedAt = user.CreatedAt,
            };

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新医护信息失败");
            return StatusCode(500, "更新医护信息失败");
        }
    }

    /// <summary>
    /// 删除医护信息（软删除）
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin,HeadNurse")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteMedicalStaff(Guid id)
    {
        try
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound("医护信息不存在");
            }

            user.IsDeleted = true;
            await _context.SaveChangesAsync();

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "删除医护信息失败");
            return StatusCode(500, "删除医护信息失败");
        }
    }
}

