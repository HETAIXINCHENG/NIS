namespace NisSystem.API.DTOs;

/// <summary>
/// 护理评估DTO
/// </summary>
public class NursingAssessmentDto
{
    public Guid PatientId { get; set; }
    public string AssessmentType { get; set; } = string.Empty; // Braden、GCS、VAS、跌倒风险、营养评估
    public int Score { get; set; }
    public Dictionary<string, object>? Details { get; set; } // 评估详情
    public string RiskLevel { get; set; } = "低风险";
    public DateTime? NextAssessmentDate { get; set; }
}

/// <summary>
/// Braden压疮风险评估详情
/// </summary>
public class BradenAssessmentDetails
{
    public int SensoryPerception { get; set; } // 感觉知觉 1-4
    public int Moisture { get; set; } // 潮湿 1-4
    public int Activity { get; set; } // 活动 1-4
    public int Mobility { get; set; } // 移动 1-4
    public int Nutrition { get; set; } // 营养 1-4
    public int FrictionAndShear { get; set; } // 摩擦力和剪切力 1-3
    public int TotalScore { get; set; } // 总分
}

/// <summary>
/// GCS昏迷评分详情
/// </summary>
public class GcsAssessmentDetails
{
    public int EyeOpening { get; set; } // 睁眼反应 1-4
    public int VerbalResponse { get; set; } // 语言反应 1-5
    public int MotorResponse { get; set; } // 运动反应 1-6
    public int TotalScore { get; set; } // 总分 3-15
}

/// <summary>
/// VAS疼痛评分详情
/// </summary>
public class VasAssessmentDetails
{
    public int Score { get; set; } // 0-10分
    public string? Location { get; set; } // 疼痛部位
    public string? Description { get; set; } // 疼痛描述
}

/// <summary>
/// 跌倒风险评估详情
/// </summary>
public class FallRiskAssessmentDetails
{
    public int Age { get; set; } // 年龄因素
    public int History { get; set; } // 跌倒史
    public int Medication { get; set; } // 用药因素
    public int Mobility { get; set; } // 移动能力
    public int Cognition { get; set; } // 认知状态
    public int TotalScore { get; set; } // 总分
}

/// <summary>
/// 营养评估详情
/// </summary>
public class NutritionAssessmentDetails
{
    public decimal? Bmi { get; set; } // BMI
    public decimal? Weight { get; set; } // 体重
    public decimal? Height { get; set; } // 身高
    public string? Appetite { get; set; } // 食欲
    public string? EatingAbility { get; set; } // 进食能力
    public int TotalScore { get; set; } // 总分
}

