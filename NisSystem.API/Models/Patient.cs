using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NisSystem.API.Models;

/// <summary>
/// 患者实体
/// </summary>
[Table("Patients")]
public class Patient : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Gender { get; set; } = string.Empty; // 男/女
    public int Age { get; set; }
    public string AdmissionNumber { get; set; } = string.Empty; // 住院号
    public string? Diagnosis { get; set; } // 入院诊断（加密存储）
    public string? Allergies { get; set; } // 过敏史（加密存储）
    public string? MedicationHistory { get; set; } // 用药史（加密存储）
    public string? ContactInfo { get; set; } // 家属联系信息（加密存储）
    public string? SpecialCareNeeds { get; set; } // 特殊护理需求
    public string NursingLevel { get; set; } = "三级"; // 护理级别：特级、一级、二级、三级
    public string? Department { get; set; } // 科室
    public string? RoomNumber { get; set; } // 房间号
    public string? BedNumber { get; set; } // 床位号
    public DateTime? AdmissionDate { get; set; } // 入院日期
    public DateTime? DischargeDate { get; set; } // 出院日期
    public string Status { get; set; } = "在院"; // 在院/转科/出院
    
    // 外键
    public Guid? AssignedNurseId { get; set; } // 责任护士
    
    // 导航属性
    public User? AssignedNurse { get; set; }
    public ICollection<VitalSigns> VitalSigns { get; set; } = new List<VitalSigns>();
    public ICollection<NursingAssessment> NursingAssessments { get; set; } = new List<NursingAssessment>();
    public ICollection<NursingRecord> NursingRecords { get; set; } = new List<NursingRecord>();
    public ICollection<MedicationOrder> MedicationOrders { get; set; } = new List<MedicationOrder>();
    public ICollection<NursingPlan> NursingPlans { get; set; } = new List<NursingPlan>();
}

