using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NisSystem.API.Data;
using NisSystem.API.Models;
using System.Security.Claims;

namespace NisSystem.API.Controllers;

/// <summary>
/// 护理计划控制器
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class NursingPlansController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<NursingPlansController> _logger;

    public NursingPlansController(ApplicationDbContext context, ILogger<NursingPlansController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// 获取患者的所有护理计划
    /// </summary>
    [HttpGet("patient/{patientId}")]
    [ProducesResponseType(typeof(List<NursingPlan>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPlansByPatient(Guid patientId, [FromQuery] string? status)
    {
        var query = _context.NursingPlans
            .Include(p => p.CreatedByUser)
            .Where(p => p.PatientId == patientId)
            .AsQueryable();

        if (!string.IsNullOrEmpty(status))
        {
            query = query.Where(p => p.Status == status);
        }

        var plans = await query
            .OrderByDescending(p => p.StartDate)
            .ToListAsync();

        // 清除循环引用
        foreach (var plan in plans)
        {
            if (plan.Patient != null)
            {
                plan.Patient.NursingPlans = new List<NursingPlan>();
            }
        }

        return Ok(plans);
    }

    /// <summary>
    /// 创建个性化护理计划
    /// </summary>
    [HttpPost("personalized")]
    [Authorize(Roles = "Admin,Nurse,HeadNurse")]
    [ProducesResponseType(typeof(NursingPlan), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreatePersonalizedPlan([FromBody] CreateNursingPlanDto dto)
    {
        return await CreatePlan(dto, "个性化");
    }

    /// <summary>
    /// 创建标准化护理路径
    /// </summary>
    [HttpPost("standardized")]
    [Authorize(Roles = "Admin,Nurse,HeadNurse")]
    [ProducesResponseType(typeof(NursingPlan), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateStandardizedPlan([FromBody] CreateNursingPlanDto dto)
    {
        return await CreatePlan(dto, "标准化路径");
    }

    /// <summary>
    /// 基于评估结果自动生成护理计划
    /// </summary>
    [HttpPost("auto-generate/{patientId}")]
    [Authorize(Roles = "Admin,Nurse,HeadNurse")]
    [ProducesResponseType(typeof(NursingPlan), StatusCodes.Status201Created)]
    public async Task<IActionResult> AutoGeneratePlan(Guid patientId)
    {
        // 获取最新的评估结果
        var latestAssessments = await _context.NursingAssessments
            .Where(a => a.PatientId == patientId)
            .GroupBy(a => a.AssessmentType)
            .Select(g => g.OrderByDescending(a => a.AssessedAt).First())
            .ToListAsync();

        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized();
        }

        // 根据评估结果生成护理目标和措施
        var goals = new List<string>();
        var measures = new List<string>();

        foreach (var assessment in latestAssessments)
        {
            if (assessment.RiskLevel == "高风险")
            {
                switch (assessment.AssessmentType)
                {
                    case "Braden":
                        goals.Add("预防压疮发生");
                        measures.Add("每2小时翻身一次，保持皮肤清洁干燥");
                        break;
                    case "跌倒风险":
                        goals.Add("预防跌倒发生");
                        measures.Add("床栏保护，地面防滑，加强巡视");
                        break;
                    case "营养评估":
                        goals.Add("改善营养状况");
                        measures.Add("加强营养支持，监测体重变化");
                        break;
                }
            }
        }

        var plan = new NursingPlan
        {
            Id = Guid.NewGuid(),
            PatientId = patientId,
            CreatedByUserId = userId,
            PlanType = "个性化",
            Title = "基于评估结果的护理计划",
            Goals = string.Join("；", goals),
            Measures = JsonSerializer.Serialize(measures),
            StartDate = DateTime.UtcNow,
            Status = "进行中",
            CreatedAt = DateTime.UtcNow,
            CreatedBy = User.Identity?.Name
        };

        _context.NursingPlans.Add(plan);
        await _context.SaveChangesAsync();

        await _context.Entry(plan).Reference(p => p.CreatedByUser).LoadAsync();

        return CreatedAtAction(nameof(GetPlansByPatient), new { patientId }, plan);
    }

    /// <summary>
    /// 更新护理计划
    /// </summary>
    [HttpPut("{id}")]
    [Authorize(Roles = "Admin,Nurse,HeadNurse")]
    [ProducesResponseType(typeof(NursingPlan), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdatePlan(Guid id, [FromBody] UpdateNursingPlanDto dto)
    {
        var plan = await _context.NursingPlans.FindAsync(id);
        if (plan == null)
        {
            return NotFound();
        }

        plan.Title = dto.Title ?? plan.Title;
        plan.NursingDiagnosis = dto.NursingDiagnosis ?? plan.NursingDiagnosis;
        plan.ExistingProblems = dto.ExistingProblems ?? plan.ExistingProblems;
        plan.Goals = dto.Goals ?? plan.Goals;
        plan.Measures = dto.Measures != null ? JsonSerializer.Serialize(dto.Measures) : plan.Measures;
        plan.Status = dto.Status ?? plan.Status;
        plan.Evaluation = dto.Evaluation ?? plan.Evaluation;
        plan.RectificationOpinions = dto.RectificationOpinions ?? plan.RectificationOpinions;
        plan.Signature = dto.Signature ?? plan.Signature;
        plan.EndDate = dto.EndDate.HasValue ? DateTime.SpecifyKind(dto.EndDate.Value, DateTimeKind.Utc) : plan.EndDate;
        plan.UpdatedAt = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc);
        plan.UpdatedBy = User.Identity?.Name;

        await _context.SaveChangesAsync();

        return Ok(plan);
    }

    /// <summary>
    /// 创建护理计划通用方法
    /// </summary>
    private async Task<IActionResult> CreatePlan(CreateNursingPlanDto dto, string planType)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized();
        }

        var patient = await _context.Patients.FindAsync(dto.PatientId);
        if (patient == null)
        {
            return NotFound(new { message = "患者不存在" });
        }

        // 解析日期
        DateTime startDate = DateTime.UtcNow;
        if (!string.IsNullOrEmpty(dto.PlanDate))
        {
            if (DateTime.TryParse(dto.PlanDate, out var parsedDate))
            {
                startDate = DateTime.SpecifyKind(parsedDate, DateTimeKind.Utc);
            }
        }
        else if (dto.StartDate.HasValue)
        {
            startDate = DateTime.SpecifyKind(dto.StartDate.Value, DateTimeKind.Utc);
        }

        var plan = new NursingPlan
        {
            Id = Guid.NewGuid(),
            PatientId = dto.PatientId,
            CreatedByUserId = userId,
            PlanType = planType,
            Title = dto.Title,
            NursingDiagnosis = dto.NursingDiagnosis,
            ExistingProblems = dto.ExistingProblems,
            Goals = dto.Goals ?? string.Empty,
            Measures = dto.Measures != null ? JsonSerializer.Serialize(dto.Measures) : string.Empty,
            StartDate = startDate,
            EndDate = dto.EndDate.HasValue ? DateTime.SpecifyKind(dto.EndDate.Value, DateTimeKind.Utc) : (DateTime?)null,
            Status = "进行中",
            Evaluation = dto.Evaluation,
            RectificationOpinions = dto.RectificationOpinions,
            Signature = dto.Signature,
            CreatedAt = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc),
            CreatedBy = User.Identity?.Name
        };

        _context.NursingPlans.Add(plan);
        await _context.SaveChangesAsync();

        await _context.Entry(plan).Reference(p => p.CreatedByUser).LoadAsync();
        if (plan.Patient != null)
        {
            plan.Patient.NursingPlans = new List<NursingPlan>();
        }

        return CreatedAtAction(nameof(GetPlansByPatient), new { patientId = dto.PatientId }, plan);
    }
}

/// <summary>
/// 创建护理计划DTO
/// </summary>
public class CreateNursingPlanDto
{
    public Guid PatientId { get; set; }
    public string PlanDate { get; set; } = string.Empty; // YYYY-MM-DD
    public string? NursingDiagnosis { get; set; }
    public string? ExistingProblems { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Goals { get; set; }
    public List<string>? Measures { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? Evaluation { get; set; }
    public string? RectificationOpinions { get; set; }
    public string? Signature { get; set; }
}

/// <summary>
/// 更新护理计划DTO
/// </summary>
public class UpdateNursingPlanDto
{
    public string? Title { get; set; }
    public string? NursingDiagnosis { get; set; }
    public string? ExistingProblems { get; set; }
    public string? Goals { get; set; }
    public List<string>? Measures { get; set; }
    public string? Status { get; set; }
    public string? Evaluation { get; set; }
    public string? RectificationOpinions { get; set; }
    public string? Signature { get; set; }
    public DateTime? EndDate { get; set; }
}

