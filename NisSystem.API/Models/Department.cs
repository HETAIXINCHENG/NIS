using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NisSystem.API.Models;

/// <summary>
/// 科室实体
/// </summary>
[Table("Departments")]
public class Department : BaseEntity
{
    /// <summary>
    /// 科室名称
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 科室代码
    /// </summary>
    [MaxLength(50)]
    public string? Code { get; set; }

    /// <summary>
    /// 科室描述
    /// </summary>
    [MaxLength(500)]
    public string? Description { get; set; }

    /// <summary>
    /// 科室类型（临床科室、医技科室、行政科室等）
    /// </summary>
    [MaxLength(50)]
    public string? Type { get; set; }

    /// <summary>
    /// 科室负责人
    /// </summary>
    [MaxLength(100)]
    public string? Director { get; set; }

    /// <summary>
    /// 联系电话
    /// </summary>
    [MaxLength(50)]
    public string? Phone { get; set; }

    /// <summary>
    /// 科室位置
    /// </summary>
    [MaxLength(200)]
    public string? Location { get; set; }

    /// <summary>
    /// 是否启用
    /// </summary>
    public bool IsActive { get; set; } = true;

}

