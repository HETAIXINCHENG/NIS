using NisSystem.API.DTOs;

namespace NisSystem.API.Services;

/// <summary>
/// 认证服务接口
/// </summary>
public interface IAuthService
{
    Task<AuthResponseDto?> LoginAsync(LoginDto loginDto);
    Task<bool> RegisterAsync(RegisterDto registerDto);
}

