using System.ComponentModel.DataAnnotations;

namespace NisSystem.API.DTOs;

/// <summary>
/// 创建医护信息DTO
/// </summary>
public class CreateMedicalStaffDto
{
    [Required(ErrorMessage = "用户名不能为空")]
    [MaxLength(50)]
    public string Username { get; set; } = string.Empty;

    [MaxLength(50)]
    public string? Password { get; set; }

    [Required(ErrorMessage = "姓名不能为空")]
    [MaxLength(50)]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "工号不能为空")]
    [MaxLength(50)]
    public string EmployeeId { get; set; } = string.Empty;

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

