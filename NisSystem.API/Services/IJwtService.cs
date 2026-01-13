using NisSystem.API.Models;

namespace NisSystem.API.Services;

/// <summary>
/// JWT 服务接口
/// </summary>
public interface IJwtService
{
    string GenerateToken(User user);
    bool ValidateToken(string token);
}

