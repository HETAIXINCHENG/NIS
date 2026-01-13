namespace NisSystem.API.DTOs;

/// <summary>
/// 登录请求DTO
/// </summary>
public class LoginDto
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

