using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NisSystem.API.Models;

/// <summary>
/// 药物准备记录
/// </summary>
[Table("MedicationPreparations")]
public class MedicationPreparation : BaseEntity
{
    // 外键
    public Guid MedicationOrderId { get; set; }
    public Guid PreparedByUserId { get; set; }
    
    public string PreparationType { get; set; } = string.Empty; // 配制、准备
    public string PreparationContent { get; set; } = string.Empty; // 配制内容
    public DateTime PreparedAt { get; set; } = DateTime.UtcNow;
    public bool HasContraindication { get; set; } = false; // 是否有配伍禁忌
    public bool HasAllergyWarning { get; set; } = false; // 是否有过敏警告
    public string? Notes { get; set; } // 备注
    
    // 导航属性
    public MedicationOrder MedicationOrder { get; set; } = null!;
    public User PreparedBy { get; set; } = null!;
}
