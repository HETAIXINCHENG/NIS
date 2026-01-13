using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NisSystem.API.DTOs;
using NisSystem.API.Services;
using System.Security.Claims;

namespace NisSystem.API.Controllers;

/// <summary>
/// 智能提醒控制器
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class NotificationsController : ControllerBase
{
    private readonly INotificationService _notificationService;
    private readonly ILogger<NotificationsController> _logger;

    public NotificationsController(
        INotificationService notificationService,
        ILogger<NotificationsController> logger)
    {
        _notificationService = notificationService;
        _logger = logger;
    }

    /// <summary>
    /// 获取所有通知
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<NotificationDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllNotifications()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        Guid? userId = null;
        if (!string.IsNullOrEmpty(userIdClaim) && Guid.TryParse(userIdClaim, out var parsedUserId))
        {
            userId = parsedUserId;
        }

        var notifications = await _notificationService.GetAllNotificationsAsync(userId);
        return Ok(notifications);
    }

    /// <summary>
    /// 获取用药提醒
    /// </summary>
    [HttpGet("medications")]
    [Authorize(Roles = "Admin,Nurse,HeadNurse")]
    [ProducesResponseType(typeof(List<NotificationDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMedicationReminders()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        Guid? userId = null;
        if (!string.IsNullOrEmpty(userIdClaim) && Guid.TryParse(userIdClaim, out var parsedUserId))
        {
            userId = parsedUserId;
        }

        var reminders = await _notificationService.GetMedicationRemindersAsync(userId);
        return Ok(reminders);
    }

    /// <summary>
    /// 获取评估提醒
    /// </summary>
    [HttpGet("assessments")]
    [Authorize(Roles = "Admin,Nurse,HeadNurse")]
    [ProducesResponseType(typeof(List<NotificationDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAssessmentReminders()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        Guid? userId = null;
        if (!string.IsNullOrEmpty(userIdClaim) && Guid.TryParse(userIdClaim, out var parsedUserId))
        {
            userId = parsedUserId;
        }

        var reminders = await _notificationService.GetAssessmentRemindersAsync(userId);
        return Ok(reminders);
    }

    /// <summary>
    /// 获取风险预警
    /// </summary>
    [HttpGet("risks")]
    [Authorize(Roles = "Admin,Nurse,HeadNurse,Doctor")]
    [ProducesResponseType(typeof(List<NotificationDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRiskWarnings()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        Guid? userId = null;
        if (!string.IsNullOrEmpty(userIdClaim) && Guid.TryParse(userIdClaim, out var parsedUserId))
        {
            userId = parsedUserId;
        }

        var warnings = await _notificationService.GetRiskWarningsAsync(userId);
        return Ok(warnings);
    }

    /// <summary>
    /// 获取医嘱执行超时提醒
    /// </summary>
    [HttpGet("overdue")]
    [Authorize(Roles = "Admin,Nurse,HeadNurse")]
    [ProducesResponseType(typeof(List<NotificationDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetOverdueMedications()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        Guid? userId = null;
        if (!string.IsNullOrEmpty(userIdClaim) && Guid.TryParse(userIdClaim, out var parsedUserId))
        {
            userId = parsedUserId;
        }

        var overdue = await _notificationService.GetOverdueMedicationRemindersAsync(userId);
        return Ok(overdue);
    }
}

