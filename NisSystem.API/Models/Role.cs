using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NisSystem.API.Models;

/// <summary>
/// 角色实体
/// </summary>
[Table("Roles")]
public class Role : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Permissions { get; set; } = string.Empty; // JSON格式存储权限列表
    
    // 导航属性
    public ICollection<User> Users { get; set; } = new List<User>();
}

