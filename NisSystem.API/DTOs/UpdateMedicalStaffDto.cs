using System.ComponentModel.DataAnnotations;

namespace NisSystem.API.DTOs;

/// <summary>
/// 更新医护信息DTO
/// </summary>
public class UpdateMedicalStaffDto
{
    [MaxLength(50)]
    public string? Username { get; set; }

    [MaxLength(50)]
    public string? Password { get; set; }

    [MaxLength(50)]
    public string? Name { get; set; }

    [MaxLength(50)]
    public string? EmployeeId { get; set; }

    [EmailAddress(ErrorMessage = "邮箱格式不正确")]
    [MaxLength(100)]
    public string? Email { get; set; }

    [MaxLength(20)]
    public string? Phone { get; set; }

    public Guid? DepartmentId { get; set; }

    [MaxLength(100)]
    public string? DepartmentName { get; set; }

    [MaxLength(50)]
    public string? Position { get; set; }

    public bool? IsActive { get; set; }
}

