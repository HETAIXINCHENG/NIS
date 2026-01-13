using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NisSystem.API.Models;

/// <summary>
/// 护理计划
/// </summary>
[Table("NursingPlans")]
public class NursingPlan : BaseEntity
{
    // 外键
    public Guid PatientId { get; set; }
    public Guid? CreatedByUserId { get; set; }
    
    public string PlanType { get; set; } = "个性化"; // 个性化/标准化路径
    public string Title { get; set; } = string.Empty;
    public string? NursingDiagnosis { get; set; } // 护理诊断
    public string? ExistingProblems { get; set; } // 存在问题
    public string Goals { get; set; } = string.Empty; // 护理目标
    public string Measures { get; set; } = string.Empty; // 护理措施（JSON格式）
    public DateTime StartDate { get; set; } = DateTime.UtcNow;
    public DateTime? EndDate { get; set; }
    public string Status { get; set; } = "进行中"; // 进行中/已完成/已取消
    public string? Evaluation { get; set; } // 效果评价
    public string? RectificationOpinions { get; set; } // 护理部整改意见及持续改进
    public string? Signature { get; set; } // 签名
    
    // 导航属性
    public Patient Patient { get; set; } = null!;
    public User? CreatedByUser { get; set; }
}

