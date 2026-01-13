using System.ComponentModel.DataAnnotations;

namespace NisSystem.API.DTOs;

/// <summary>
/// 创建科室DTO
/// </summary>
public class CreateDepartmentDto
{
    [Required(ErrorMessage = "科室名称不能为空")]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(50)]
    public string? Code { get; set; }

    [MaxLength(500)]
    public string? Description { get; set; }

    [MaxLength(50)]
    public string? Type { get; set; }

    [MaxLength(100)]
    public string? Director { get; set; }

    [MaxLength(50)]
    public string? Phone { get; set; }

    [MaxLength(200)]
    public string? Location { get; set; }

    public bool? IsActive { get; set; }
}

