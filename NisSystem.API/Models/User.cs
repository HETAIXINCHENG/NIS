using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NisSystem.API.Models;

/// <summary>
/// 用户实体（护士、护师、护长等）
/// </summary>
[Table("Users")]
public class User : BaseEntity
{
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty; // BCrypt加密
    public string Name { get; set; } = string.Empty;
    public string EmployeeId { get; set; } = string.Empty; // 工号
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Department { get; set; } // 科室（保留用于兼容，新数据使用DepartmentId）
    public Guid? DepartmentId { get; set; } // 科室外键
    public string? Position { get; set; } // 职位：护士、护师、护长等
    public bool IsActive { get; set; } = true;
    public DateTime? LastLoginAt { get; set; }
    
    // 外键
    public Guid RoleId { get; set; }
    
    // 导航属性
    public Role Role { get; set; } = null!;
    public Department? DepartmentNavigation { get; set; }
    public ICollection<Patient> Patients { get; set; } = new List<Patient>();
    public ICollection<NursingRecord> NursingRecords { get; set; } = new List<NursingRecord>();
    public ICollection<MedicationExecution> MedicationExecutions { get; set; } = new List<MedicationExecution>();
    public ICollection<ShiftHandover> ShiftHandovers { get; set; } = new List<ShiftHandover>();
    public ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();
}

