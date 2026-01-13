using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NisSystem.API.Models;

/// <summary>
/// 排班记录
/// </summary>
[Table("Schedules")]
public class Schedule : BaseEntity
{
    // 外键
    public Guid UserId { get; set; }
    
    [Column(TypeName = "timestamp with time zone")]
    public DateTime WorkDate { get; set; }
    public string ShiftType { get; set; } = string.Empty; // 白班/夜班/休息
    public TimeSpan? StartTime { get; set; }
    public TimeSpan? EndTime { get; set; }
    public decimal? ActualWorkHours { get; set; } // 实际工作时间
    public bool IsOvertime { get; set; } = false; // 是否加班
    public string? Department { get; set; } // 科室
    
    // 导航属性
    public User User { get; set; } = null!;
}

