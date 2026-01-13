using System.ComponentModel.DataAnnotations;

namespace NisSystem.API.DTOs;

/// <summary>
/// 创建护理评估 DTO
/// </summary>
public class CreateNursingAssessmentDto
{
    [Required(ErrorMessage = "患者ID不能为空")]
    public Guid PatientId { get; set; }
    
    // 一般资料
    public string? Department { get; set; }
    public string? BedNumber { get; set; }
    public string? Occupation { get; set; }
    public string? EducationLevel { get; set; }
    public DateTime? AdmissionDate { get; set; }
    public TimeSpan? AdmissionTime { get; set; }
    // 支持字符串格式的时间（前端发送 "HH:mm" 格式）
    public string? AdmissionTimeString { get; set; }
    public List<string>? AdmissionMethod { get; set; }
    public string? AdmissionDiagnosis { get; set; }
    
    // 护理评估 - 意识与语言
    public List<string>? Consciousness { get; set; }
    public string? ConsciousnessOther { get; set; }
    public List<string>? SpeechExpression { get; set; }
    public string? SpeechExpressionOther { get; set; }
    
    // 护理评估 - 视力听力
    public List<string>? VisionLeft { get; set; }
    public string? VisionLeftOther { get; set; }
    public List<string>? VisionRight { get; set; }
    public string? VisionRightOther { get; set; }
    public List<string>? HearingLeft { get; set; }
    public string? HearingLeftOther { get; set; }
    public List<string>? HearingRight { get; set; }
    public string? HearingRightOther { get; set; }
    
    // 护理评估 - 口腔皮肤
    public List<string>? OralMucosa { get; set; }
    public string? OralMucosaOther { get; set; }
    public List<string>? Skin { get; set; }
    public string? SkinOther { get; set; }
    public string? Dentures { get; set; }
    
    // 护理评估 - 压疮
    public string? PressureUlcerRisk { get; set; }
    public int? BradenScore { get; set; }
    
    // 护理评估 - 排泄
    public List<string>? Urine { get; set; }
    public string? UrineOther { get; set; }
    public List<string>? Stool { get; set; }
    public string? StoolTimesPerDay { get; set; }
    public string? StoolOther { get; set; }
    public List<string>? ExcretionOther { get; set; }
    public string? ExcretionOtherDetail { get; set; }
    
    // 护理评估 - 舒适
    public List<string>? Pain { get; set; }
    public string? PainLocation { get; set; }
    public string? PainOther { get; set; }
    
    // 护理评估 - 自理与风险
    public string? SelfCareAbility { get; set; }
    public string? FallRisk { get; set; }
    public int? FallRiskScore { get; set; }
    
    // 护理评估 - 心理
    public List<string>? Psychological { get; set; }
    public string? PsychologicalOther { get; set; }
    public string? SuicidalTendency { get; set; }
    
    // 护理评估 - 生活习惯
    public string? Smoking { get; set; }
    public string? Drinking { get; set; }
    public List<string>? Diet { get; set; }
    public string? DietOther { get; set; }
    public List<string>? FoodRestrictions { get; set; }
    public string? FoodRestrictionsDetail { get; set; }
    public List<string>? Sleep { get; set; }
    public string? SleepMedication { get; set; }
    
    // 护理评估 - 病史
    public List<string>? PastMedicalHistory { get; set; }
    public string? PastMedicalHistoryOther { get; set; }
    public List<string>? FamilyHistory { get; set; }
    public string? FamilyHistoryDetail { get; set; }
    public string? AllergyHistory { get; set; }
    public string? AllergyMedication { get; set; }
    public string? AllergyFood { get; set; }
    public string? AllergyOther { get; set; }
    public List<string>? MedicalExpenses { get; set; }
    
    // 入院宣教
    public List<string>? AdmissionEducation { get; set; }
    public string? AdmissionReason { get; set; }
    
    // 签名
    public string? NarratorSignature { get; set; }
    public string? NarratorRelationship { get; set; }
    public string? NurseSignature { get; set; }
    public DateTime? AssessmentDate { get; set; }
    public TimeSpan? AssessmentTime { get; set; }
    // 支持字符串格式的时间（前端发送 "HH:mm" 格式）
    public string? AssessmentTimeString { get; set; }
}

