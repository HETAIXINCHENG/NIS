using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NisSystem.API.Data;

namespace NisSystem.API.Controllers;

/// <summary>
/// 报表统计控制器
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ReportsController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ReportsController> _logger;

    public ReportsController(ApplicationDbContext context, ILogger<ReportsController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// 护理工作量统计
    /// </summary>
    [HttpGet("workload")]
    [Authorize(Roles = "Admin,HeadNurse,Doctor")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetWorkloadStatistics(
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate,
        [FromQuery] Guid? userId,
        [FromQuery] string? department)
    {
        var start = startDate.HasValue 
            ? DateTime.SpecifyKind(startDate.Value, DateTimeKind.Utc)
            : DateTime.UtcNow.AddDays(-30);
        var end = endDate.HasValue
            ? DateTime.SpecifyKind(endDate.Value, DateTimeKind.Utc)
            : DateTime.UtcNow;

        var query = _context.NursingRecords.AsQueryable();
        if (userId.HasValue)
        {
            query = query.Where(r => r.RecordedByUserId == userId.Value);
        }

        var records = await query
            .Where(r => r.RecordedAt >= start && r.RecordedAt <= end)
            .ToListAsync();

        var statistics = new
        {
            TotalRecords = records.Count,
            RecordsByType = records.GroupBy(r => r.RecordType)
                .Select(g => new { Type = g.Key, Count = g.Count() })
                .ToList(),
            RecordsByDate = records.GroupBy(r => r.RecordedAt.Date)
                .Select(g => new { Date = g.Key, Count = g.Count() })
                .OrderBy(x => x.Date)
                .ToList()
        };

        return Ok(statistics);
    }

    /// <summary>
    /// 患者分类统计
    /// </summary>
    [HttpGet("patients")]
    [Authorize(Roles = "Admin,HeadNurse,Doctor")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPatientStatistics([FromQuery] string? department)
    {
        var query = _context.Patients.AsQueryable();
        if (!string.IsNullOrEmpty(department))
        {
            query = query.Where(p => p.Department == department);
        }

        var patients = await query.ToListAsync();

        var statistics = new
        {
            Total = patients.Count,
            ByStatus = patients.GroupBy(p => p.Status)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToList(),
            ByNursingLevel = patients.GroupBy(p => p.NursingLevel)
                .Select(g => new { Level = g.Key, Count = g.Count() })
                .ToList(),
            ByDepartment = patients.GroupBy(p => p.Department)
                .Select(g => new { Department = g.Key, Count = g.Count() })
                .ToList()
        };

        return Ok(statistics);
    }

    /// <summary>
    /// 用药执行统计
    /// </summary>
    [HttpGet("medications")]
    [Authorize(Roles = "Admin,HeadNurse,Doctor")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMedicationStatistics(
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate)
    {
        var start = startDate.HasValue
            ? DateTime.SpecifyKind(startDate.Value, DateTimeKind.Utc)
            : DateTime.UtcNow.AddDays(-30);
        var end = endDate.HasValue
            ? DateTime.SpecifyKind(endDate.Value, DateTimeKind.Utc)
            : DateTime.UtcNow;

        var executions = await _context.MedicationExecutions
            .Include(e => e.MedicationOrder)
            .Where(e => e.ExecutedTime >= start && e.ExecutedTime <= end)
            .ToListAsync();

        var statistics = new
        {
            TotalExecutions = executions.Count,
            ByStatus = executions.GroupBy(e => e.Status)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToList(),
            ExecutionsByDate = executions
                .Where(e => e.ExecutedTime.HasValue)
                .GroupBy(e => e.ExecutedTime.Value.Date)
                .Select(g => new { Date = g.Key, Count = g.Count() })
                .OrderBy(x => x.Date)
                .ToList(),
            OnTimeRate = executions.Count > 0
                ? (double)executions.Count(e => e.ExecutedTime.HasValue &&
                    Math.Abs((e.ExecutedTime.Value - e.ScheduledTime).TotalMinutes) <= 30) / executions.Count * 100
                : 0,
            VerifiedRate = executions.Count > 0
                ? (double)executions.Count(e => e.IsVerified) / executions.Count * 100
                : 0
        };

        return Ok(statistics);
    }

    /// <summary>
    /// 护理质量指标
    /// </summary>
    [HttpGet("quality")]
    [Authorize(Roles = "Admin,HeadNurse,Doctor")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetQualityIndicators(
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate)
    {
        var start = startDate.HasValue
            ? DateTime.SpecifyKind(startDate.Value, DateTimeKind.Utc)
            : DateTime.UtcNow.AddDays(-30);
        var end = endDate.HasValue
            ? DateTime.SpecifyKind(endDate.Value, DateTimeKind.Utc)
            : DateTime.UtcNow;

        // 压疮发生率
        var pressureUlcerAssessments = await _context.NursingAssessments
            .Where(a => a.AssessmentType == "Braden" &&
                       a.AssessedAt >= start && a.AssessedAt <= end)
            .ToListAsync();
        var pressureUlcerRate = pressureUlcerAssessments.Count > 0
            ? (double)pressureUlcerAssessments.Count(a => a.RiskLevel == "高风险") / pressureUlcerAssessments.Count * 100
            : 0;

        // 跌倒发生率
        var fallRiskAssessments = await _context.NursingAssessments
            .Where(a => a.AssessmentType == "跌倒风险" &&
                       a.AssessedAt >= start && a.AssessedAt <= end)
            .ToListAsync();
        var fallRiskRate = fallRiskAssessments.Count > 0
            ? (double)fallRiskAssessments.Count(a => a.RiskLevel == "高风险") / fallRiskAssessments.Count * 100
            : 0;

        // 用药差错率
        var medicationExecutions = await _context.MedicationExecutions
            .Where(e => e.ExecutedTime >= start && e.ExecutedTime <= end)
            .ToListAsync();
        var medicationErrorRate = medicationExecutions.Count > 0
            ? (double)medicationExecutions.Count(e => e.Status == "异常" || !string.IsNullOrEmpty(e.Notes)) / medicationExecutions.Count * 100
            : 0;

        // 感染控制指标
        var patients = await _context.Patients
            .Where(p => p.AdmissionDate >= start && p.AdmissionDate <= end)
            .ToListAsync();
        var totalPatientDays = patients.Sum(p => (end - (p.AdmissionDate ?? start)).Days);
        var infectionRecords = await _context.ConditionObservations
            .Where(c => c.ObservationType == "症状记录" &&
                       c.Content.Contains("感染") &&
                       c.ObservedAt >= start && c.ObservedAt <= end)
            .ToListAsync();
        var infectionRate = totalPatientDays > 0
            ? (double)infectionRecords.Count / totalPatientDays * 1000 // 千分率
            : 0;

        var statistics = new
        {
            PressureUlcerRate = Math.Round(pressureUlcerRate, 2),
            FallRiskRate = Math.Round(fallRiskRate, 2),
            MedicationErrorRate = Math.Round(medicationErrorRate, 2),
            InfectionRate = Math.Round(infectionRate, 2), // 感染率（千分率）
            TotalAssessments = pressureUlcerAssessments.Count + fallRiskAssessments.Count,
            TotalMedicationExecutions = medicationExecutions.Count,
            TotalPatientDays = totalPatientDays,
            InfectionCount = infectionRecords.Count
        };

        return Ok(statistics);
    }

    /// <summary>
    /// 护理文书质控
    /// </summary>
    [HttpGet("document-quality")]
    [Authorize(Roles = "Admin,HeadNurse")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDocumentQuality(
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate)
    {
        var start = startDate.HasValue
            ? DateTime.SpecifyKind(startDate.Value, DateTimeKind.Utc)
            : DateTime.UtcNow.AddDays(-30);
        var end = endDate.HasValue
            ? DateTime.SpecifyKind(endDate.Value, DateTimeKind.Utc)
            : DateTime.UtcNow;

        // 记录完整性检查
        var vitalSignsRecords = await _context.VitalSigns
            .Where(v => v.RecordedAt >= start && v.RecordedAt <= end)
            .ToListAsync();
        var completeVitalSigns = vitalSignsRecords.Count(v =>
            v.Temperature.HasValue && v.Pulse.HasValue &&
            v.Respiration.HasValue && v.SystolicBP.HasValue);

        var nursingRecords = await _context.NursingRecords
            .Where(r => r.RecordedAt >= start && r.RecordedAt <= end)
            .ToListAsync();
        var completeNursingRecords = nursingRecords.Count(r =>
            !string.IsNullOrEmpty(r.Content) && !string.IsNullOrEmpty(r.RecordType));

        // 时效性监控（记录是否及时）
        var timelyRecords = vitalSignsRecords.Count(v =>
            (DateTime.UtcNow - v.RecordedAt).TotalHours <= 24);

        // 书写规范检查（检查是否有必要的字段）
        var standardizedRecords = nursingRecords.Count(r =>
            !string.IsNullOrEmpty(r.Content) &&
            !string.IsNullOrEmpty(r.RecordType) &&
            r.RecordedAt != default);

        var statistics = new
        {
            VitalSignsCompleteness = vitalSignsRecords.Count > 0
                ? (double)completeVitalSigns / vitalSignsRecords.Count * 100
                : 0,
            NursingRecordsCompleteness = nursingRecords.Count > 0
                ? (double)completeNursingRecords / nursingRecords.Count * 100
                : 0,
            Timeliness = vitalSignsRecords.Count > 0
                ? (double)timelyRecords / vitalSignsRecords.Count * 100
                : 0,
            Standardization = nursingRecords.Count > 0
                ? (double)standardizedRecords / nursingRecords.Count * 100
                : 0,
            TotalVitalSignsRecords = vitalSignsRecords.Count,
            TotalNursingRecords = nursingRecords.Count
        };

        return Ok(statistics);
    }

    /// <summary>
    /// 成本效益分析
    /// </summary>
    [HttpGet("cost-benefit")]
    [Authorize(Roles = "Admin,HeadNurse,Doctor")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCostBenefitAnalysis(
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate,
        [FromQuery] string? department)
    {
        var start = startDate.HasValue
            ? DateTime.SpecifyKind(startDate.Value, DateTimeKind.Utc)
            : DateTime.UtcNow.AddDays(-30);
        var end = endDate.HasValue
            ? DateTime.SpecifyKind(endDate.Value, DateTimeKind.Utc)
            : DateTime.UtcNow;

        var query = _context.Patients.AsQueryable();
        if (!string.IsNullOrEmpty(department))
        {
            query = query.Where(p => p.Department == department);
        }

        var patients = await query
            .Where(p => p.AdmissionDate >= start && p.AdmissionDate <= end)
            .ToListAsync();

        // 计算平均住院天数
        var avgLengthOfStay = patients.Count > 0
            ? patients.Where(p => p.AdmissionDate.HasValue && p.DischargeDate.HasValue)
                .Average(p => (p.DischargeDate.Value - p.AdmissionDate.Value).Days)
            : 0;

        // 计算护理工作量
        var nursingRecords = await _context.NursingRecords
            .Where(r => r.RecordedAt >= start && r.RecordedAt <= end)
            .ToListAsync();

        // 计算用药成本（简化处理）
        var medicationOrders = await _context.MedicationOrders
            .Where(o => o.OrderDate >= start && o.OrderDate <= end)
            .ToListAsync();

        var statistics = new
        {
            TotalPatients = patients.Count,
            AvgLengthOfStay = Math.Round(avgLengthOfStay, 2),
            TotalNursingRecords = nursingRecords.Count,
            AvgRecordsPerPatient = patients.Count > 0
                ? Math.Round((double)nursingRecords.Count / patients.Count, 2)
                : 0,
            TotalMedicationOrders = medicationOrders.Count,
            CostEfficiency = patients.Count > 0
                ? Math.Round((double)nursingRecords.Count / patients.Count, 2) // 护理工作量/患者数
                : 0
        };

        return Ok(statistics);
    }

    /// <summary>
    /// 人员配置分析
    /// </summary>
    [HttpGet("staffing")]
    [Authorize(Roles = "Admin,HeadNurse")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetStaffingAnalysis(
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate,
        [FromQuery] string? department)
    {
        var start = startDate.HasValue
            ? DateTime.SpecifyKind(startDate.Value, DateTimeKind.Utc)
            : DateTime.UtcNow.AddDays(-30);
        var end = endDate.HasValue
            ? DateTime.SpecifyKind(endDate.Value, DateTimeKind.Utc)
            : DateTime.UtcNow;

        // 获取护士信息
        var nursesQuery = _context.Users
            .Where(u => u.IsActive && !u.IsDeleted &&
                       (u.Position == "护士" || u.Position == "护师" || u.Position == "护长"))
            .AsQueryable();

        if (!string.IsNullOrEmpty(department))
        {
            nursesQuery = nursesQuery.Where(u => u.Department == department);
        }

        var nurses = await nursesQuery.ToListAsync();

        // 获取排班信息
        var schedules = await _context.Schedules
            .Where(s => s.WorkDate >= start && s.WorkDate <= end)
            .ToListAsync();

        // 获取患者数量
        var patientsQuery = _context.Patients.AsQueryable();
        if (!string.IsNullOrEmpty(department))
        {
            patientsQuery = patientsQuery.Where(p => p.Department == department);
        }

        var patients = await patientsQuery
            .Where(p => p.AdmissionDate >= start && p.AdmissionDate <= end)
            .ToListAsync();

        // 计算工作量
        var nursingRecords = await _context.NursingRecords
            .Where(r => r.RecordedAt >= start && r.RecordedAt <= end)
            .ToListAsync();

        var statistics = new
        {
            TotalNurses = nurses.Count,
            NursesByPosition = nurses.GroupBy(n => n.Position)
                .Select(g => new { Position = g.Key, Count = g.Count() })
                .ToList(),
            TotalWorkHours = schedules.Sum(s => s.ActualWorkHours ?? 0),
            AvgWorkHoursPerNurse = nurses.Count > 0
                ? Math.Round((double)schedules.Sum(s => s.ActualWorkHours ?? 0) / nurses.Count, 2)
                : 0,
            PatientToNurseRatio = nurses.Count > 0
                ? Math.Round((double)patients.Count / nurses.Count, 2)
                : 0,
            RecordsPerNurse = nurses.Count > 0
                ? Math.Round((double)nursingRecords.Count / nurses.Count, 2)
                : 0,
            OvertimeHours = schedules.Where(s => s.IsOvertime).Sum(s => s.ActualWorkHours ?? 0)
        };

        return Ok(statistics);
    }
}
