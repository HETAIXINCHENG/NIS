using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NisSystem.API.Data;
using NisSystem.API.Models;

namespace NisSystem.API.Controllers;

/// <summary>
/// 护理效果跟踪控制器
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class NursingEffectTrackingController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<NursingEffectTrackingController> _logger;

    public NursingEffectTrackingController(ApplicationDbContext context, ILogger<NursingEffectTrackingController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// 获取护理计划效果跟踪
    /// </summary>
    [HttpGet("plan/{planId}")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPlanEffectTracking(Guid planId)
    {
        var plan = await _context.NursingPlans
            .Include(p => p.Patient)
            .FirstOrDefaultAsync(p => p.Id == planId);

        if (plan == null)
        {
            return NotFound();
        }

        // 获取相关的评估记录
        var assessments = await _context.NursingAssessments
            .Where(a => a.PatientId == plan.PatientId &&
                       a.AssessedAt >= plan.StartDate)
            .OrderBy(a => a.AssessedAt)
            .ToListAsync();

        // 获取相关的护理记录
        var nursingRecords = await _context.NursingRecords
            .Where(r => r.PatientId == plan.PatientId &&
                       r.RecordedAt >= plan.StartDate)
            .OrderBy(r => r.RecordedAt)
            .ToListAsync();

        // 获取生命体征趋势
        var vitalSigns = await _context.VitalSigns
            .Where(v => v.PatientId == plan.PatientId &&
                       v.RecordedAt >= plan.StartDate)
            .OrderBy(v => v.RecordedAt)
            .ToListAsync();

        var result = new
        {
            Plan = new
            {
                plan.Id,
                plan.Title,
                plan.Goals,
                plan.Status,
                plan.StartDate,
                plan.EndDate,
                plan.Evaluation
            },
            Assessments = assessments.Select(a => new
            {
                a.AssessmentType,
                a.Score,
                a.RiskLevel,
                a.AssessedAt
            }),
            NursingRecordsCount = nursingRecords.Count,
            VitalSignsTrend = vitalSigns.Select(v => new
            {
                v.RecordedAt,
                v.Temperature,
                v.Pulse,
                v.Respiration,
                v.SystolicBP,
                v.DiastolicBP,
                v.IsAbnormal
            }),
            Effectiveness = CalculateEffectiveness(plan, assessments, vitalSigns)
        };

        return Ok(result);
    }

    /// <summary>
    /// 计算护理效果
    /// </summary>
    private string CalculateEffectiveness(
        NursingPlan plan,
        List<NursingAssessment> assessments,
        List<VitalSigns> vitalSigns)
    {
        if (assessments.Count < 2)
        {
            return "数据不足，无法评估";
        }

        // 比较首次和最新评估
        var firstAssessment = assessments.First();
        var latestAssessment = assessments.Last();

        // 如果风险等级降低，说明有效
        if (firstAssessment.RiskLevel == "高风险" && latestAssessment.RiskLevel != "高风险")
        {
            return "有效";
        }

        // 如果生命体征异常值减少，说明有效
        var earlyAbnormalCount = vitalSigns.Take(vitalSigns.Count / 2).Count(v => v.IsAbnormal);
        var lateAbnormalCount = vitalSigns.Skip(vitalSigns.Count / 2).Count(v => v.IsAbnormal);

        if (lateAbnormalCount < earlyAbnormalCount)
        {
            return "有效";
        }

        if (lateAbnormalCount > earlyAbnormalCount)
        {
            return "需调整";
        }

        return "持续观察";
    }

    /// <summary>
    /// 获取患者护理效果汇总
    /// </summary>
    [HttpGet("patient/{patientId}")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPatientEffectSummary(Guid patientId, [FromQuery] DateTime? startDate)
    {
        var start = startDate ?? DateTime.UtcNow.AddDays(-30);

        var plans = await _context.NursingPlans
            .Where(p => p.PatientId == patientId && p.StartDate >= start)
            .ToListAsync();

        var assessments = await _context.NursingAssessments
            .Where(a => a.PatientId == patientId && a.AssessedAt >= start)
            .ToListAsync();

        var summary = new
        {
            TotalPlans = plans.Count,
            ActivePlans = plans.Count(p => p.Status == "进行中"),
            CompletedPlans = plans.Count(p => p.Status == "已完成"),
            TotalAssessments = assessments.Count,
            RiskLevelDistribution = assessments.GroupBy(a => a.RiskLevel)
                .Select(g => new { RiskLevel = g.Key, Count = g.Count() })
                .ToList(),
            PlansWithEvaluation = plans.Count(p => !string.IsNullOrEmpty(p.Evaluation))
        };

        return Ok(summary);
    }
}

