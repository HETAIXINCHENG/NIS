using System.ComponentModel.DataAnnotations;

namespace NisSystem.API.DTOs;

/// <summary>
/// 系统集成DTO
/// </summary>
public class SystemIntegrationDto
{
    public Guid Id { get; set; }
    public string SystemName { get; set; } = string.Empty;
    public string IntegrationType { get; set; } = string.Empty;
    public string RequestData { get; set; } = string.Empty;
    public string? ResponseData { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime IntegratedAt { get; set; }
    public string? ErrorMessage { get; set; }
    public Guid? RelatedPatientId { get; set; }
    public Guid? RelatedOrderId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

/// <summary>
/// 创建系统集成DTO
/// </summary>
public class CreateSystemIntegrationDto
{
    [Required(ErrorMessage = "系统名称不能为空")]
    [MaxLength(100)]
    public string SystemName { get; set; } = string.Empty;

    [Required(ErrorMessage = "集成类型不能为空")]
    [MaxLength(100)]
    public string IntegrationType { get; set; } = string.Empty;

    [Required(ErrorMessage = "请求数据不能为空")]
    public string RequestData { get; set; } = string.Empty;

    public string? ResponseData { get; set; }

    [MaxLength(50)]
    public string Status { get; set; } = "成功";

    public DateTime? IntegratedAt { get; set; }

    [MaxLength(500)]
    public string? ErrorMessage { get; set; }

    public Guid? RelatedPatientId { get; set; }

    public Guid? RelatedOrderId { get; set; }
}

/// <summary>
/// 更新系统集成DTO
/// </summary>
public class UpdateSystemIntegrationDto
{
    [MaxLength(100)]
    public string? SystemName { get; set; }

    [MaxLength(100)]
    public string? IntegrationType { get; set; }

    public string? RequestData { get; set; }

    public string? ResponseData { get; set; }

    [MaxLength(50)]
    public string? Status { get; set; }

    public DateTime? IntegratedAt { get; set; }

    [MaxLength(500)]
    public string? ErrorMessage { get; set; }

    public Guid? RelatedPatientId { get; set; }

    public Guid? RelatedOrderId { get; set; }
}

