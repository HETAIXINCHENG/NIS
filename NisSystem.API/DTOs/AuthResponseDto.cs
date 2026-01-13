namespace NisSystem.API.DTOs;

/// <summary>
/// 认证响应DTO
/// </summary>
public class AuthResponseDto
{
    public string Token { get; set; } = string.Empty;
    public Guid UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string EmployeeId { get; set; } = string.Empty;
    public string? Department { get; set; }
    public string? Position { get; set; }
}

