using System.ComponentModel.DataAnnotations;

namespace NisSystem.API.DTOs;

/// <summary>
/// 系统设置DTO
/// </summary>
public class SystemSettingsDto
{
    public Guid Id { get; set; }
    public string HospitalName { get; set; } = string.Empty;
    public string HospitalCode { get; set; } = string.Empty;
    public string? Address { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Website { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

/// <summary>
/// 创建/更新系统设置DTO
/// </summary>
public class CreateOrUpdateSystemSettingsDto
{
    [Required(ErrorMessage = "医院名称不能为空")]
    [MaxLength(200)]
    public string HospitalName { get; set; } = string.Empty;

    [Required(ErrorMessage = "医院编码不能为空")]
    [MaxLength(50)]
    public string HospitalCode { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Address { get; set; }

    [MaxLength(50)]
    public string? Phone { get; set; }

    [MaxLength(100)]
    [EmailAddress(ErrorMessage = "邮箱格式不正确")]
    public string? Email { get; set; }

    [MaxLength(200)]
    public string? Website { get; set; }

    [MaxLength(500)]
    public string? Description { get; set; }
}

