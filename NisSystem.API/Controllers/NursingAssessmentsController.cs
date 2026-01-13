using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NisSystem.API.Data;
using NisSystem.API.DTOs;
using NisSystem.API.Models;
using System.Security.Claims;

namespace NisSystem.API.Controllers;

/// <summary>
/// 护理评估控制器
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class NursingAssessmentsController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<NursingAssessmentsController> _logger;

    public NursingAssessmentsController(ApplicationDbContext context, ILogger<NursingAssessmentsController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// 获取患者的所有评估记录
    /// </summary>
    [HttpGet("patient/{patientId}")]
    [ProducesResponseType(typeof(List<NursingAssessment>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAssessmentsByPatient(Guid patientId, [FromQuery] string? assessmentType)
    {
        var query = _context.NursingAssessments
            .Include(a => a.AssessedBy)
            .Where(a => a.PatientId == patientId)
            .AsQueryable();

        if (!string.IsNullOrEmpty(assessmentType))
        {
            query = query.Where(a => a.AssessmentType == assessmentType);
        }

        var assessments = await query
            .OrderByDescending(a => a.AssessedAt)
            .ToListAsync();

        // 清除循环引用
        foreach (var assessment in assessments)
        {
            if (assessment.Patient != null)
            {
                assessment.Patient.NursingAssessments = new List<NursingAssessment>();
            }
        }

        return Ok(assessments);
    }

    /// <summary>
    /// 创建入院护理评估
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Admin,Nurse,HeadNurse")]
    [ProducesResponseType(typeof(NursingAssessment), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateNursingAssessment([FromBody] CreateNursingAssessmentDto dto)
    {
        try
        {
            _logger.LogInformation("CreateNursingAssessment: 收到创建请求");

            if (dto == null)
            {
                _logger.LogWarning("CreateNursingAssessment: 请求体为空");
                return BadRequest(new { message = "请求数据不能为空" });
            }

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("CreateNursingAssessment: 模型验证失败");
                var errors = ModelState
                    .Where(x => x.Value?.Errors.Count > 0)
                    .ToDictionary(
                        kvp => kvp.Key,
                        kvp => kvp.Value?.Errors.Select(e => e.ErrorMessage).ToArray()
                    );
                _logger.LogWarning("验证错误详情: {Errors}", System.Text.Json.JsonSerializer.Serialize(errors));
                return BadRequest(new { 
                    message = "数据验证失败", 
                    errors = errors,
                    title = "One or more validation errors occurred."
                });
            }

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                _logger.LogWarning("CreateNursingAssessment: 用户ID无效");
                return Unauthorized();
            }

            // 验证患者是否存在
            var patient = await _context.Patients.FindAsync(dto.PatientId);
            if (patient == null)
            {
                _logger.LogWarning("CreateNursingAssessment: 患者不存在, PatientId: {PatientId}", dto.PatientId);
                return BadRequest(new { message = "患者不存在" });
            }

            // 创建评估实体
            var assessment = new NursingAssessment
            {
                Id = Guid.NewGuid(),
                PatientId = dto.PatientId,
                AssessedByUserId = userId,
                AssessmentType = "入院评估",
                AssessedAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = User.Identity?.Name,
                
                // 一般资料
                Department = dto.Department,
                BedNumber = dto.BedNumber,
                Occupation = dto.Occupation,
                EducationLevel = dto.EducationLevel,
                AdmissionDate = dto.AdmissionDate.HasValue ? DateTime.SpecifyKind(dto.AdmissionDate.Value, DateTimeKind.Utc) : null,
                AdmissionTime = dto.AdmissionTime ?? ParseTimeSpan(dto.AdmissionTimeString),
                AdmissionMethod = dto.AdmissionMethod != null ? JsonSerializer.Serialize(dto.AdmissionMethod) : null,
                AdmissionDiagnosis = dto.AdmissionDiagnosis,
                
                // 护理评估 - 意识与语言
                Consciousness = dto.Consciousness != null ? JsonSerializer.Serialize(dto.Consciousness) : null,
                ConsciousnessOther = dto.ConsciousnessOther,
                SpeechExpression = dto.SpeechExpression != null ? JsonSerializer.Serialize(dto.SpeechExpression) : null,
                SpeechExpressionOther = dto.SpeechExpressionOther,
                
                // 护理评估 - 视力听力
                VisionLeft = dto.VisionLeft != null ? JsonSerializer.Serialize(dto.VisionLeft) : null,
                VisionLeftOther = dto.VisionLeftOther,
                VisionRight = dto.VisionRight != null ? JsonSerializer.Serialize(dto.VisionRight) : null,
                VisionRightOther = dto.VisionRightOther,
                HearingLeft = dto.HearingLeft != null ? JsonSerializer.Serialize(dto.HearingLeft) : null,
                HearingLeftOther = dto.HearingLeftOther,
                HearingRight = dto.HearingRight != null ? JsonSerializer.Serialize(dto.HearingRight) : null,
                HearingRightOther = dto.HearingRightOther,
                
                // 护理评估 - 口腔皮肤
                OralMucosa = dto.OralMucosa != null ? JsonSerializer.Serialize(dto.OralMucosa) : null,
                OralMucosaOther = dto.OralMucosaOther,
                Skin = dto.Skin != null ? JsonSerializer.Serialize(dto.Skin) : null,
                SkinOther = dto.SkinOther,
                Dentures = dto.Dentures,
                
                // 护理评估 - 压疮
                PressureUlcerRisk = dto.PressureUlcerRisk,
                BradenScore = dto.BradenScore,
                
                // 护理评估 - 排泄
                Urine = dto.Urine != null ? JsonSerializer.Serialize(dto.Urine) : null,
                UrineOther = dto.UrineOther,
                Stool = dto.Stool != null ? JsonSerializer.Serialize(dto.Stool) : null,
                StoolTimesPerDay = dto.StoolTimesPerDay,
                StoolOther = dto.StoolOther,
                ExcretionOther = dto.ExcretionOther != null ? JsonSerializer.Serialize(dto.ExcretionOther) : null,
                ExcretionOtherDetail = dto.ExcretionOtherDetail,
                
                // 护理评估 - 舒适
                Pain = dto.Pain != null ? JsonSerializer.Serialize(dto.Pain) : null,
                PainLocation = dto.PainLocation,
                PainOther = dto.PainOther,
                
                // 护理评估 - 自理与风险
                SelfCareAbility = dto.SelfCareAbility,
                FallRisk = dto.FallRisk,
                FallRiskScore = dto.FallRiskScore,
                
                // 护理评估 - 心理
                Psychological = dto.Psychological != null ? JsonSerializer.Serialize(dto.Psychological) : null,
                PsychologicalOther = dto.PsychologicalOther,
                SuicidalTendency = dto.SuicidalTendency,
                
                // 护理评估 - 生活习惯
                Smoking = dto.Smoking,
                Drinking = dto.Drinking,
                Diet = dto.Diet != null ? JsonSerializer.Serialize(dto.Diet) : null,
                DietOther = dto.DietOther,
                FoodRestrictions = dto.FoodRestrictions != null ? JsonSerializer.Serialize(dto.FoodRestrictions) : null,
                FoodRestrictionsDetail = dto.FoodRestrictionsDetail,
                Sleep = dto.Sleep != null ? JsonSerializer.Serialize(dto.Sleep) : null,
                SleepMedication = dto.SleepMedication,
                
                // 护理评估 - 病史
                PastMedicalHistory = dto.PastMedicalHistory != null ? JsonSerializer.Serialize(dto.PastMedicalHistory) : null,
                PastMedicalHistoryOther = dto.PastMedicalHistoryOther,
                FamilyHistory = dto.FamilyHistory != null ? JsonSerializer.Serialize(dto.FamilyHistory) : null,
                FamilyHistoryDetail = dto.FamilyHistoryDetail,
                AllergyHistory = dto.AllergyHistory,
                AllergyMedication = dto.AllergyMedication,
                AllergyFood = dto.AllergyFood,
                AllergyOther = dto.AllergyOther,
                MedicalExpenses = dto.MedicalExpenses != null ? JsonSerializer.Serialize(dto.MedicalExpenses) : null,
                
                // 入院宣教
                AdmissionEducation = dto.AdmissionEducation != null ? JsonSerializer.Serialize(dto.AdmissionEducation) : null,
                AdmissionReason = dto.AdmissionReason,
                
                // 签名
                NarratorSignature = dto.NarratorSignature,
                NarratorRelationship = dto.NarratorRelationship,
                NurseSignature = dto.NurseSignature,
                AssessmentDate = dto.AssessmentDate.HasValue ? DateTime.SpecifyKind(dto.AssessmentDate.Value, DateTimeKind.Utc) : null,
                AssessmentTime = dto.AssessmentTime ?? ParseTimeSpan(dto.AssessmentTimeString),
            };

            _context.NursingAssessments.Add(assessment);
            await _context.SaveChangesAsync();

            // 重新加载数据
            await _context.Entry(assessment).Reference(a => a.AssessedBy).LoadAsync();
            if (assessment.Patient != null)
            {
                assessment.Patient.NursingAssessments = new List<NursingAssessment>();
            }

            _logger.LogInformation("CreateNursingAssessment: 成功创建护理评估, Id: {Id}, PatientId: {PatientId}", assessment.Id, assessment.PatientId);
            return CreatedAtAction(nameof(GetAssessmentsByPatient), new { patientId = assessment.PatientId }, assessment);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CreateNursingAssessment: 创建护理评估时发生错误");
            return StatusCode(500, new { message = "创建护理评估失败", error = ex.Message });
        }
    }

    /// <summary>
    /// 获取最新的评估记录
    /// </summary>
    [HttpGet("patient/{patientId}/latest")]
    [ProducesResponseType(typeof(Dictionary<string, NursingAssessment>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetLatestAssessments(Guid patientId)
    {
        var assessmentTypes = new[] { "Braden", "GCS", "VAS", "跌倒风险", "营养评估" };
        var latestAssessments = new Dictionary<string, NursingAssessment?>();

        foreach (var type in assessmentTypes)
        {
            var assessment = await _context.NursingAssessments
                .Include(a => a.AssessedBy)
                .Where(a => a.PatientId == patientId && a.AssessmentType == type)
                .OrderByDescending(a => a.AssessedAt)
                .FirstOrDefaultAsync();

            latestAssessments[type] = assessment;
        }

        return Ok(latestAssessments);
    }

    /// <summary>
    /// 创建Braden压疮风险评估
    /// </summary>
    [HttpPost("braden")]
    [Authorize(Roles = "Admin,Nurse,HeadNurse")]
    [ProducesResponseType(typeof(NursingAssessment), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateBradenAssessment([FromBody] NursingAssessmentDto dto)
    {
        return await CreateAssessment(dto, "Braden", CalculateBradenRisk);
    }

    /// <summary>
    /// 创建GCS昏迷评分
    /// </summary>
    [HttpPost("gcs")]
    [Authorize(Roles = "Admin,Nurse,HeadNurse")]
    [ProducesResponseType(typeof(NursingAssessment), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateGcsAssessment([FromBody] NursingAssessmentDto dto)
    {
        return await CreateAssessment(dto, "GCS", CalculateGcsRisk);
    }

    /// <summary>
    /// 创建VAS疼痛评分
    /// </summary>
    [HttpPost("vas")]
    [Authorize(Roles = "Admin,Nurse,HeadNurse")]
    [ProducesResponseType(typeof(NursingAssessment), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateVasAssessment([FromBody] NursingAssessmentDto dto)
    {
        return await CreateAssessment(dto, "VAS", CalculateVasRisk);
    }

    /// <summary>
    /// 创建跌倒风险评估
    /// </summary>
    [HttpPost("fall-risk")]
    [Authorize(Roles = "Admin,Nurse,HeadNurse")]
    [ProducesResponseType(typeof(NursingAssessment), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateFallRiskAssessment([FromBody] NursingAssessmentDto dto)
    {
        return await CreateAssessment(dto, "跌倒风险", CalculateFallRisk);
    }

    /// <summary>
    /// 创建营养评估
    /// </summary>
    [HttpPost("nutrition")]
    [Authorize(Roles = "Admin,Nurse,HeadNurse")]
    [ProducesResponseType(typeof(NursingAssessment), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateNutritionAssessment([FromBody] NursingAssessmentDto dto)
    {
        return await CreateAssessment(dto, "营养评估", CalculateNutritionRisk);
    }

    /// <summary>
    /// 通用创建评估方法
    /// </summary>
    private async Task<IActionResult> CreateAssessment(
        NursingAssessmentDto dto,
        string assessmentType,
        Func<int, string> riskCalculator)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized();
        }

        // 验证患者存在
        var patient = await _context.Patients.FindAsync(dto.PatientId);
        if (patient == null)
        {
            return NotFound(new { message = "患者不存在" });
        }

        var assessment = new NursingAssessment
        {
            Id = Guid.NewGuid(),
            PatientId = dto.PatientId,
            AssessedByUserId = userId,
            AssessmentType = assessmentType,
            Score = dto.Score,
            Details = dto.Details != null ? JsonSerializer.Serialize(dto.Details) : null,
            RiskLevel = riskCalculator(dto.Score),
            AssessedAt = DateTime.UtcNow,
            NextAssessmentDate = dto.NextAssessmentDate,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = User.Identity?.Name
        };

        _context.NursingAssessments.Add(assessment);
        await _context.SaveChangesAsync();

        // 加载关联数据
        await _context.Entry(assessment).Reference(a => a.AssessedBy).LoadAsync();
        if (assessment.Patient != null)
        {
            assessment.Patient.NursingAssessments = new List<NursingAssessment>();
        }

        return CreatedAtAction(nameof(GetAssessmentsByPatient), new { patientId = dto.PatientId }, assessment);
    }

    // 风险评估计算方法
    private string CalculateBradenRisk(int score) => score switch
    {
        <= 12 => "高风险",
        <= 14 => "中风险",
        _ => "低风险"
    };

    private string CalculateGcsRisk(int score) => score switch
    {
        <= 8 => "高风险（昏迷）",
        <= 12 => "中风险",
        _ => "低风险"
    };

    private string CalculateVasRisk(int score) => score switch
    {
        >= 7 => "高风险（重度疼痛）",
        >= 4 => "中风险（中度疼痛）",
        _ => "低风险（轻度疼痛）"
    };

    private string CalculateFallRisk(int score) => score switch
    {
        >= 45 => "高风险",
        >= 25 => "中风险",
        _ => "低风险"
    };

    private string CalculateNutritionRisk(int score) => score switch
    {
        <= 3 => "高风险（营养不良）",
        <= 6 => "中风险",
        _ => "低风险"
    };

    /// <summary>
    /// 将字符串格式的时间（"HH:mm"）转换为 TimeSpan
    /// </summary>
    private TimeSpan? ParseTimeSpan(string? timeString)
    {
        if (string.IsNullOrWhiteSpace(timeString))
        {
            return null;
        }

        if (TimeSpan.TryParse(timeString, out var timeSpan))
        {
            return timeSpan;
        }

        // 尝试解析 "HH:mm" 格式
        var parts = timeString.Split(':');
        if (parts.Length == 2 && 
            int.TryParse(parts[0], out var hours) && 
            int.TryParse(parts[1], out var minutes))
        {
            return new TimeSpan(hours, minutes, 0);
        }

        return null;
    }
}
