using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NisSystem.API.Models;

/// <summary>
/// 护理评估
/// </summary>
[Table("NursingAssessments")]
public class NursingAssessment : BaseEntity
{
    // 外键
    public Guid PatientId { get; set; }
    public Guid AssessedByUserId { get; set; }
    
    // 一般资料
    public string? Department { get; set; } // 科别
    public string? BedNumber { get; set; } // 床号
    public string? Occupation { get; set; } // 职业
    public string? EducationLevel { get; set; } // 文化
    public DateTime? AdmissionDate { get; set; } // 入院/转入日期
    public TimeSpan? AdmissionTime { get; set; } // 入院/转入时间
    public string? AdmissionMethod { get; set; } // 入院方式（JSON数组）
    public string? AdmissionDiagnosis { get; set; } // 入院诊断
    
    // 护理评估 - 意识与语言
    public string? Consciousness { get; set; } // 意识（JSON数组）
    public string? ConsciousnessOther { get; set; } // 意识-其他
    public string? SpeechExpression { get; set; } // 语言表达（JSON数组）
    public string? SpeechExpressionOther { get; set; } // 语言表达-其他
    
    // 护理评估 - 视力听力
    public string? VisionLeft { get; set; } // 视力-左眼（JSON数组）
    public string? VisionLeftOther { get; set; } // 视力-左眼-其他
    public string? VisionRight { get; set; } // 视力-右眼（JSON数组）
    public string? VisionRightOther { get; set; } // 视力-右眼-其他
    public string? HearingLeft { get; set; } // 听力-左耳（JSON数组）
    public string? HearingLeftOther { get; set; } // 听力-左耳-其他
    public string? HearingRight { get; set; } // 听力-右耳（JSON数组）
    public string? HearingRightOther { get; set; } // 听力-右耳-其他
    
    // 护理评估 - 口腔皮肤
    public string? OralMucosa { get; set; } // 口腔黏膜（JSON数组）
    public string? OralMucosaOther { get; set; } // 口腔黏膜-其他
    public string? Skin { get; set; } // 皮肤（JSON数组）
    public string? SkinOther { get; set; } // 皮肤-其他
    public string? Dentures { get; set; } // 义齿
    
    // 护理评估 - 压疮
    public string? PressureUlcerRisk { get; set; } // 压疮高危
    public int? BradenScore { get; set; } // Braden评分总分
    
    // 护理评估 - 排泄
    public string? Urine { get; set; } // 排泄情况-小便（JSON数组）
    public string? UrineOther { get; set; } // 排泄情况-小便-其他
    public string? Stool { get; set; } // 排泄情况-大便（JSON数组）
    public string? StoolTimesPerDay { get; set; } // 腹泻次数/日
    public string? StoolOther { get; set; } // 排泄情况-大便-其他
    public string? ExcretionOther { get; set; } // 排泄情况-其他（JSON数组）
    public string? ExcretionOtherDetail { get; set; } // 排泄情况-其他-详情
    
    // 护理评估 - 舒适
    public string? Pain { get; set; } // 舒适-疼痛（JSON数组）
    public string? PainLocation { get; set; } // 疼痛部位
    public string? PainOther { get; set; } // 疼痛-其他
    
    // 护理评估 - 自理与风险
    public string? SelfCareAbility { get; set; } // 自理能力
    public string? FallRisk { get; set; } // 跌倒/坠床高危
    public int? FallRiskScore { get; set; } // 评分量表总分
    
    // 护理评估 - 心理
    public string? Psychological { get; set; } // 心理（JSON数组）
    public string? PsychologicalOther { get; set; } // 心理-其他
    public string? SuicidalTendency { get; set; } // 自杀倾向
    
    // 护理评估 - 生活习惯
    public string? Smoking { get; set; } // 生活习惯-吸烟
    public string? Drinking { get; set; } // 生活习惯-饮酒
    public string? Diet { get; set; } // 生活习惯-饮食（JSON数组）
    public string? DietOther { get; set; } // 饮食-其他
    public string? FoodRestrictions { get; set; } // 食物禁忌（JSON数组）
    public string? FoodRestrictionsDetail { get; set; } // 食物禁忌-详情
    public string? Sleep { get; set; } // 生活习惯-睡眠（JSON数组）
    public string? SleepMedication { get; set; } // 睡眠-药物
    
    // 护理评估 - 病史
    public string? PastMedicalHistory { get; set; } // 既往史（JSON数组）
    public string? PastMedicalHistoryOther { get; set; } // 既往史-其他
    public string? FamilyHistory { get; set; } // 家族史（JSON数组）
    public string? FamilyHistoryDetail { get; set; } // 家族史-详情
    public string? AllergyHistory { get; set; } // 过敏史
    public string? AllergyMedication { get; set; } // 过敏-药物
    public string? AllergyFood { get; set; } // 过敏-食物
    public string? AllergyOther { get; set; } // 过敏-其他
    public string? MedicalExpenses { get; set; } // 医疗费用（JSON数组）
    
    // 入院宣教
    public string? AdmissionEducation { get; set; } // 入院宣教项目（JSON数组）
    public string? AdmissionReason { get; set; } // 此次入院原因
    
    // 签名
    public string? NarratorSignature { get; set; } // 病情叙述者签名
    public string? NarratorRelationship { get; set; } // 与患者关系
    public string? NurseSignature { get; set; } // 评估护士签名
    public DateTime? AssessmentDate { get; set; } // 评估日期
    public TimeSpan? AssessmentTime { get; set; } // 评估时间
    
    // 原有字段（保留兼容性）
    public string AssessmentType { get; set; } = "入院评估"; // 评估类型
    public int Score { get; set; } // 评分
    public string? Details { get; set; } // 评估详情（JSON格式，保留用于其他评估类型）
    public string RiskLevel { get; set; } = "低风险"; // 低风险/中风险/高风险
    public DateTime AssessedAt { get; set; } = DateTime.UtcNow;
    public DateTime? NextAssessmentDate { get; set; } // 下次评估日期
    
    // 导航属性
    public Patient Patient { get; set; } = null!;
    public User AssessedBy { get; set; } = null!;
}
