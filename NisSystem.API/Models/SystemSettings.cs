using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NisSystem.API.Models;

/// <summary>
/// 系统设置（基础信息）
/// </summary>
[Table("SystemSettings")]
public class SystemSettings : BaseEntity
{
    /// <summary>
    /// 医院名称
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string HospitalName { get; set; } = string.Empty;

    /// <summary>
    /// 医院编码
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string HospitalCode { get; set; } = string.Empty;

    /// <summary>
    /// 地址
    /// </summary>
    [MaxLength(500)]
    public string? Address { get; set; }

    /// <summary>
    /// 联系电话
    /// </summary>
    [MaxLength(50)]
    public string? Phone { get; set; }

    /// <summary>
    /// 邮箱
    /// </summary>
    [MaxLength(100)]
    [EmailAddress]
    public string? Email { get; set; }

    /// <summary>
    /// 网站
    /// </summary>
    [MaxLength(200)]
    public string? Website { get; set; }

    /// <summary>
    /// 医院简介
    /// </summary>
    [Column(TypeName = "text")]
    public string? Description { get; set; }

    /// <summary>
    /// 是否启用
    /// </summary>
    public bool IsActive { get; set; } = true;
}

