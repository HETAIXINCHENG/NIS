using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NisSystem.API.Models;

/// <summary>
/// 用药效果观察记录
/// </summary>
[Table("MedicationEffects")]
public class MedicationEffect : BaseEntity
{
    // 外键
    public Guid MedicationExecutionId { get; set; }
    public Guid ObservedByUserId { get; set; }
    
    public string EffectType { get; set; } = string.Empty; // 有效、无效、不良反应
    public string Observation { get; set; } = string.Empty; // 观察内容
    public DateTime ObservedAt { get; set; } = DateTime.UtcNow;
    public bool HasAdverseReaction { get; set; } = false; // 是否有不良反应
    public string? AdverseReactionDetails { get; set; } // 不良反应详情
    public string? Notes { get; set; } // 备注
    
    // 导航属性
    public MedicationExecution MedicationExecution { get; set; } = null!;
    public User ObservedBy { get; set; } = null!;
}
