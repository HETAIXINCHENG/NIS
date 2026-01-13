namespace NisSystem.API.DTOs;

/// <summary>
/// 注册请求DTO
/// </summary>
public class RegisterDto
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string EmployeeId { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Department { get; set; }
    public string? Position { get; set; }
    public string? RoleName { get; set; }
}

