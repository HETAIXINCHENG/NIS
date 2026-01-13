using NisSystem.API.DTOs;

namespace NisSystem.API.Services;

/// <summary>
/// 智能提醒服务接口
/// </summary>
public interface INotificationService
{
    Task<List<NotificationDto>> GetMedicationRemindersAsync(Guid? userId);
    Task<List<NotificationDto>> GetAssessmentRemindersAsync(Guid? userId);
    Task<List<NotificationDto>> GetRiskWarningsAsync(Guid? userId);
    Task<List<NotificationDto>> GetOverdueMedicationRemindersAsync(Guid? userId);
    Task<List<NotificationDto>> GetAllNotificationsAsync(Guid? userId);
}

