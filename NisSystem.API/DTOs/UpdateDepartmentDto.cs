using System.ComponentModel.DataAnnotations;

namespace NisSystem.API.DTOs;

/// <summary>
/// 更新科室DTO
/// </summary>
public class UpdateDepartmentDto
{
    [MaxLength(100)]
    public string? Name { get; set; }

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

