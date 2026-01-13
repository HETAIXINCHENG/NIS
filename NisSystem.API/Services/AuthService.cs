using Microsoft.EntityFrameworkCore;
using NisSystem.API.Data;
using NisSystem.API.DTOs;
using NisSystem.API.Models;
using BCrypt.Net;

namespace NisSystem.API.Services;

/// <summary>
/// 认证服务实现
/// </summary>
public class AuthService : IAuthService
{
    private readonly ApplicationDbContext _context;
    private readonly IJwtService _jwtService;

    public AuthService(ApplicationDbContext context, IJwtService jwtService)
    {
        _context = context;
        _jwtService = jwtService;
    }

    public async Task<AuthResponseDto?> LoginAsync(LoginDto loginDto)
    {
        var user = await _context.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Username == loginDto.Username && !u.IsDeleted);

        if (user == null || !user.IsActive)
            return null;

        if (!BCrypt.Net.BCrypt.Verify(loginDto.Password, user.PasswordHash))
            return null;

        // 更新最后登录时间
        user.LastLoginAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        var token = _jwtService.GenerateToken(user);

        return new AuthResponseDto
        {
            Token = token,
            UserId = user.Id,
            Username = user.Username,
            Name = user.Name,
            Role = user.Role?.Name ?? "User",
            EmployeeId = user.EmployeeId,
            Department = user.Department,
            Position = user.Position
        };
    }

    public async Task<bool> RegisterAsync(RegisterDto registerDto)
    {
        // 检查用户名是否已存在
        if (await _context.Users.AnyAsync(u => u.Username == registerDto.Username))
            return false;

        // 检查工号是否已存在
        if (await _context.Users.AnyAsync(u => u.EmployeeId == registerDto.EmployeeId))
            return false;

        // 获取默认角色（如果没有指定）
        var role = await _context.Roles.FirstOrDefaultAsync(r => r.Name == registerDto.RoleName);
        if (role == null)
        {
            // 创建默认角色
            role = new Role
            {
                Name = registerDto.RoleName ?? "Nurse",
                Description = "护士",
                Permissions = "[]"
            };
            _context.Roles.Add(role);
            await _context.SaveChangesAsync();
        }

        var user = new User
        {
            Username = registerDto.Username,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(registerDto.Password),
            Name = registerDto.Name,
            EmployeeId = registerDto.EmployeeId,
            Email = registerDto.Email,
            Phone = registerDto.Phone,
            Department = registerDto.Department,
            Position = registerDto.Position,
            RoleId = role.Id,
            IsActive = true
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        return true;
    }
}

