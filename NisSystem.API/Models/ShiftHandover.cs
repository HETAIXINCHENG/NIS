using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NisSystem.API.Models;

/// <summary>
/// 交接班记录
/// </summary>
[Table("ShiftHandovers")]
public class ShiftHandover : BaseEntity
{
    // 外键
    public Guid HandoverFromUserId { get; set; } // 交班人
    public Guid HandoverToUserId { get; set; } // 接班人
    
    public DateTime ShiftDate { get; set; } = DateTime.UtcNow;
    public string ShiftType { get; set; } = string.Empty; // 白班/夜班
    public string Content { get; set; } = string.Empty; // 交接内容（JSON格式，包含患者列表、注意事项等）
    public string? ImportantNotes { get; set; } // 特殊注意事项
    public string? UnfinishedTasks { get; set; } // 未完成事项
    public string? EquipmentHandover { get; set; } // 物品交接清单
    
    // 导航属性
    public User HandoverFrom { get; set; } = null!;
    public User HandoverTo { get; set; } = null!;
}

