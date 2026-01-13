using Microsoft.EntityFrameworkCore;
using NisSystem.API.Data;
using NisSystem.API.DTOs;

namespace NisSystem.API.Services;

/// <summary>
/// 智能提醒服务实现
/// </summary>
public class NotificationService : INotificationService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(ApplicationDbContext context, ILogger<NotificationService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// 获取用药时间提醒
    /// </summary>
    public async Task<List<NotificationDto>> GetMedicationRemindersAsync(Guid? userId)
    {
        var now = DateTime.UtcNow;
        var twoHoursLater = now.AddHours(2);

        var query = _context.MedicationExecutions
            .Include(e => e.MedicationOrder)
            .ThenInclude(o => o.Patient)
            .Where(e => e.Status == "待执行" &&
                       e.ScheduledTime >= now &&
                       e.ScheduledTime <= twoHoursLater)
            .AsQueryable();

        if (userId.HasValue)
        {
            query = query.Where(e => e.ExecutedByUserId == userId.Value);
        }

        var executions = await query
            .OrderBy(e => e.ScheduledTime)
            .ToListAsync();

        return executions.Select(e => new NotificationDto
        {
            Id = e.Id,
            Type = "用药提醒",
            Title = $"用药提醒：{e.MedicationOrder.MedicationName}",
            Message = $"患者 {e.MedicationOrder.Patient.Name} 需要在 {e.ScheduledTime:HH:mm} 执行用药",
            Priority = e.ScheduledTime <= now.AddMinutes(30) ? "高" : "中",
            ScheduledTime = e.ScheduledTime,
            PatientId = e.MedicationOrder.PatientId,
            PatientName = e.MedicationOrder.Patient.Name
        }).ToList();
    }

    /// <summary>
    /// 获取护理评估到期提醒
    /// </summary>
    public async Task<List<NotificationDto>> GetAssessmentRemindersAsync(Guid? userId)
    {
        var now = DateTime.UtcNow;
        var oneDayLater = now.AddDays(1);

        var query = _context.NursingAssessments
            .Include(a => a.Patient)
            .Where(a => a.NextAssessmentDate.HasValue &&
                       a.NextAssessmentDate >= now &&
                       a.NextAssessmentDate <= oneDayLater)
            .AsQueryable();

        if (userId.HasValue)
        {
            query = query.Where(a => a.AssessedByUserId == userId.Value);
        }

        var assessments = await query
            .OrderBy(a => a.NextAssessmentDate)
            .ToListAsync();

        return assessments.Select(a => new NotificationDto
        {
            Id = a.Id,
            Type = "评估提醒",
            Title = $"{a.AssessmentType}评估到期",
            Message = $"患者 {a.Patient.Name} 的 {a.AssessmentType}评估将在 {a.NextAssessmentDate:yyyy-MM-dd HH:mm} 到期",
            Priority = a.NextAssessmentDate <= now.AddHours(6) ? "高" : "中",
            ScheduledTime = a.NextAssessmentDate!.Value,
            PatientId = a.PatientId,
            PatientName = a.Patient.Name
        }).ToList();
    }

    /// <summary>
    /// 获取患者风险预警
    /// </summary>
    public async Task<List<NotificationDto>> GetRiskWarningsAsync(Guid? userId)
    {
        var query = _context.NursingAssessments
            .Include(a => a.Patient)
            .Where(a => a.RiskLevel == "高风险" &&
                       a.AssessedAt >= DateTime.UtcNow.AddDays(-7)) // 最近7天的高风险评估
            .AsQueryable();

        if (userId.HasValue)
        {
            // 获取该用户负责的患者
            var patientIds = await _context.Patients
                .Where(p => p.AssignedNurseId == userId.Value)
                .Select(p => p.Id)
                .ToListAsync();
            query = query.Where(a => patientIds.Contains(a.PatientId));
        }

        var assessments = await query
            .GroupBy(a => a.PatientId)
            .Select(g => g.OrderByDescending(a => a.AssessedAt).First())
            .ToListAsync();

        return assessments.Select(a => new NotificationDto
        {
            Id = a.Id,
            Type = "风险预警",
            Title = $"高风险患者：{a.Patient.Name}",
            Message = $"患者 {a.Patient.Name} 的 {a.AssessmentType}评估为高风险（{a.RiskLevel}），请重点关注",
            Priority = "高",
            ScheduledTime = a.AssessedAt,
            PatientId = a.PatientId,
            PatientName = a.Patient.Name
        }).ToList();
    }

    /// <summary>
    /// 获取医嘱执行超时提醒
    /// </summary>
    public async Task<List<NotificationDto>> GetOverdueMedicationRemindersAsync(Guid? userId)
    {
        var now = DateTime.UtcNow;
        var oneHourAgo = now.AddHours(-1);

        var query = _context.MedicationExecutions
            .Include(e => e.MedicationOrder)
            .ThenInclude(o => o.Patient)
            .Where(e => e.Status == "待执行" &&
                       e.ScheduledTime < now &&
                       e.ScheduledTime >= oneHourAgo)
            .AsQueryable();

        if (userId.HasValue)
        {
            query = query.Where(e => e.ExecutedByUserId == userId.Value);
        }

        var overdueExecutions = await query
            .OrderBy(e => e.ScheduledTime)
            .ToListAsync();

        return overdueExecutions.Select(e => new NotificationDto
        {
            Id = e.Id,
            Type = "医嘱超时",
            Title = $"医嘱执行超时：{e.MedicationOrder.MedicationName}",
            Message = $"患者 {e.MedicationOrder.Patient.Name} 的用药 {e.MedicationOrder.MedicationName} 应在 {e.ScheduledTime:HH:mm} 执行，现已超时",
            Priority = "高",
            ScheduledTime = e.ScheduledTime,
            PatientId = e.MedicationOrder.PatientId,
            PatientName = e.MedicationOrder.Patient.Name
        }).ToList();
    }

    /// <summary>
    /// 获取所有通知
    /// </summary>
    public async Task<List<NotificationDto>> GetAllNotificationsAsync(Guid? userId)
    {
        var medicationReminders = await GetMedicationRemindersAsync(userId);
        var assessmentReminders = await GetAssessmentRemindersAsync(userId);
        var riskWarnings = await GetRiskWarningsAsync(userId);
        var overdueReminders = await GetOverdueMedicationRemindersAsync(userId);

        var allNotifications = medicationReminders
            .Concat(assessmentReminders)
            .Concat(riskWarnings)
            .Concat(overdueReminders)
            .OrderBy(n => n.Priority == "高" ? 0 : 1)
            .ThenBy(n => n.ScheduledTime)
            .ToList();

        return allNotifications;
    }
}

/// <summary>
/// 通知DTO
/// </summary>
public class NotificationDto
{
    public Guid Id { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Priority { get; set; } = "中"; // 高/中/低
    public DateTime ScheduledTime { get; set; }
    public Guid? PatientId { get; set; }
    public string? PatientName { get; set; }
}

