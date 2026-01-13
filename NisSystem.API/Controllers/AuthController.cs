using Microsoft.AspNetCore.Mvc;
using NisSystem.API.DTOs;
using NisSystem.API.Services;

namespace NisSystem.API.Controllers;

/// <summary>
/// 认证控制器
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IAuthService authService, ILogger<AuthController> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    /// <summary>
    /// 用户登录
    /// </summary>
    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginDto loginDto)
    {
        try
        {
            var result = await _authService.LoginAsync(loginDto);
            if (result == null)
            {
                return Unauthorized(new { message = "用户名或密码错误" });
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "登录失败");
            return StatusCode(500, new { message = "登录失败，请稍后重试" });
        }
    }

    /// <summary>
    /// 用户注册
    /// </summary>
    [HttpPost("register")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Register([FromBody] RegisterDto registerDto)
    {
        try
        {
            var result = await _authService.RegisterAsync(registerDto);
            if (!result)
            {
                return BadRequest(new { message = "注册失败，用户名或工号已存在" });
            }

            return Ok(new { message = "注册成功" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "注册失败");
            return StatusCode(500, new { message = "注册失败，请稍后重试" });
        }
    }
}

