using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NisSystem.API.Data;
using NisSystem.API.DTOs;
using NisSystem.API.Models;
using System.Security.Claims;

namespace NisSystem.API.Controllers;

/// <summary>
/// 知识库控制器
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class KnowledgeBaseController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<KnowledgeBaseController> _logger;

    public KnowledgeBaseController(ApplicationDbContext context, ILogger<KnowledgeBaseController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// 获取知识库列表
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<KnowledgeBase>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetKnowledgeBases(
        [FromQuery] string? category,
        [FromQuery] string? subCategory,
        [FromQuery] string? department,
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var query = _context.KnowledgeBases
            .Where(k => k.IsActive)
            .AsQueryable();

        if (!string.IsNullOrEmpty(category))
        {
            query = query.Where(k => k.Category == category);
        }

        if (!string.IsNullOrEmpty(subCategory))
        {
            query = query.Where(k => k.SubCategory == subCategory);
        }

        if (!string.IsNullOrEmpty(department))
        {
            query = query.Where(k => k.Department == department || k.Department == null);
        }

        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(k => k.Title.Contains(search) || k.Content.Contains(search));
        }

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderByDescending(k => k.ViewCount)
            .ThenByDescending(k => k.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var result = new PagedResult<KnowledgeBase>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
        };

        return Ok(result);
    }

    /// <summary>
    /// 获取知识库详情
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(KnowledgeBase), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetKnowledgeBase(Guid id)
    {
        var knowledgeBase = await _context.KnowledgeBases.FindAsync(id);
        if (knowledgeBase == null || !knowledgeBase.IsActive)
        {
            return NotFound();
        }

        // 增加查看次数
        knowledgeBase.ViewCount++;
        await _context.SaveChangesAsync();

        return Ok(knowledgeBase);
    }

    /// <summary>
    /// 创建知识库
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Admin,HeadNurse")]
    [ProducesResponseType(typeof(KnowledgeBase), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateKnowledgeBase([FromBody] CreateKnowledgeBaseDto dto)
    {
        var knowledgeBase = new KnowledgeBase
        {
            Id = Guid.NewGuid(),
            Title = dto.Title,
            Category = dto.Category,
            SubCategory = dto.SubCategory,
            Content = dto.Content,
            Department = dto.Department,
            VideoUrl = dto.VideoUrl,
            OperationFlow = dto.OperationFlow,
            KeyPoints = dto.KeyPoints,
            Description = dto.Description,
            ImageUrls = dto.ImageUrls != null ? System.Text.Json.JsonSerializer.Serialize(dto.ImageUrls) : null,
            VideoUrls = dto.VideoUrls != null ? System.Text.Json.JsonSerializer.Serialize(dto.VideoUrls) : null,
            FileUrls = dto.Files != null ? System.Text.Json.JsonSerializer.Serialize(dto.Files) : null,
            Tags = dto.Tags != null ? string.Join(",", dto.Tags) : null,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = User.Identity?.Name
        };

        _context.KnowledgeBases.Add(knowledgeBase);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetKnowledgeBase), new { id = knowledgeBase.Id }, knowledgeBase);
    }

    /// <summary>
    /// 更新知识库
    /// </summary>
    [HttpPut("{id}")]
    [Authorize(Roles = "Admin,HeadNurse")]
    [ProducesResponseType(typeof(KnowledgeBase), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateKnowledgeBase(Guid id, [FromBody] UpdateKnowledgeBaseDto dto)
    {
        var knowledgeBase = await _context.KnowledgeBases.FindAsync(id);
        if (knowledgeBase == null)
        {
            return NotFound();
        }

        knowledgeBase.Title = dto.Title ?? knowledgeBase.Title;
        knowledgeBase.Category = dto.Category ?? knowledgeBase.Category;
        knowledgeBase.SubCategory = dto.SubCategory ?? knowledgeBase.SubCategory;
        knowledgeBase.Content = dto.Content ?? knowledgeBase.Content;
        knowledgeBase.Department = dto.Department ?? knowledgeBase.Department;
        knowledgeBase.VideoUrl = dto.VideoUrl ?? knowledgeBase.VideoUrl;
        knowledgeBase.OperationFlow = dto.OperationFlow ?? knowledgeBase.OperationFlow;
        knowledgeBase.KeyPoints = dto.KeyPoints ?? knowledgeBase.KeyPoints;
        knowledgeBase.Description = dto.Description ?? knowledgeBase.Description;
        knowledgeBase.ImageUrls = dto.ImageUrls != null ? System.Text.Json.JsonSerializer.Serialize(dto.ImageUrls) : knowledgeBase.ImageUrls;
        knowledgeBase.VideoUrls = dto.VideoUrls != null ? System.Text.Json.JsonSerializer.Serialize(dto.VideoUrls) : knowledgeBase.VideoUrls;
        knowledgeBase.FileUrls = dto.Files != null ? System.Text.Json.JsonSerializer.Serialize(dto.Files) : knowledgeBase.FileUrls;
        knowledgeBase.Tags = dto.Tags != null ? string.Join(",", dto.Tags) : knowledgeBase.Tags;
        knowledgeBase.IsActive = dto.IsActive ?? knowledgeBase.IsActive;
        knowledgeBase.UpdatedAt = DateTime.UtcNow;
        knowledgeBase.UpdatedBy = User.Identity?.Name;

        await _context.SaveChangesAsync();

        return Ok(knowledgeBase);
    }

    /// <summary>
    /// 获取护理操作规范
    /// </summary>
    [HttpGet("nursing-procedures")]
    [ProducesResponseType(typeof(List<KnowledgeBase>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetNursingProcedures([FromQuery] string? department)
    {
        var query = _context.KnowledgeBases
            .Where(k => k.IsActive && 
                       k.Category == "护理操作规范" &&
                       k.SubCategory == "标准操作流程")
            .AsQueryable();

        if (!string.IsNullOrEmpty(department))
        {
            query = query.Where(k => k.Department == department || k.Department == null);
        }

        var procedures = await query
            .OrderBy(k => k.Title)
            .ToListAsync();

        return Ok(procedures);
    }

    /// <summary>
    /// 获取临床指南
    /// </summary>
    [HttpGet("clinical-guidelines")]
    [ProducesResponseType(typeof(List<KnowledgeBase>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetClinicalGuidelines([FromQuery] string? department)
    {
        var query = _context.KnowledgeBases
            .Where(k => k.IsActive && 
                       k.Category == "临床指南")
            .AsQueryable();

        if (!string.IsNullOrEmpty(department))
        {
            query = query.Where(k => k.Department == department || k.Department == null);
        }

        var guidelines = await query
            .OrderBy(k => k.SubCategory)
            .ThenBy(k => k.Title)
            .ToListAsync();

        return Ok(guidelines);
    }

    /// <summary>
    /// 删除知识库
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin,HeadNurse")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteKnowledgeBase(Guid id)
    {
        var knowledgeBase = await _context.KnowledgeBases.FindAsync(id);
        if (knowledgeBase == null)
        {
            return NotFound();
        }

        // 硬删除：从数据库中物理删除记录
        _context.KnowledgeBases.Remove(knowledgeBase);
        await _context.SaveChangesAsync();

        _logger.LogInformation($"知识库记录已删除: {id}");

        return NoContent();
    }
}

/// <summary>
/// 创建知识库DTO
/// </summary>
public class CreateKnowledgeBaseDto
{
    public string Title { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty; // 护理操作规范、专科护理指南、应急处理流程、药物使用手册
    public string SubCategory { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string? Department { get; set; }
    public string? VideoUrl { get; set; }
    public string? OperationFlow { get; set; } // 操作流程
    public string? KeyPoints { get; set; } // 操作要点（字符串）
    public string? Description { get; set; } // 描述
    public List<string>? ImageUrls { get; set; } // 图片URLs
    public List<string>? VideoUrls { get; set; } // 视频URLs
    public List<FileInfoDto>? Files { get; set; } // 文件信息（PDF、WORD等）
    public List<string>? Tags { get; set; }
}

/// <summary>
/// 文件信息DTO
/// </summary>
public class FileInfoDto
{
    public string Name { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty; // pdf, word, image, video
}

/// <summary>
/// 更新知识库DTO
/// </summary>
public class UpdateKnowledgeBaseDto
{
    public string? Title { get; set; }
    public string? Category { get; set; }
    public string? SubCategory { get; set; }
    public string? Content { get; set; }
    public string? Department { get; set; }
    public string? VideoUrl { get; set; }
    public string? OperationFlow { get; set; }
    public string? KeyPoints { get; set; }
    public string? Description { get; set; }
    public List<string>? ImageUrls { get; set; }
    public List<string>? VideoUrls { get; set; }
    public List<FileInfoDto>? Files { get; set; }
    public List<string>? Tags { get; set; }
    public bool? IsActive { get; set; }
}
