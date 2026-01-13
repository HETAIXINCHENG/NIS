using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.IO;

namespace NisSystem.API.Controllers;

/// <summary>
/// 文件上传控制器
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class FileUploadController : ControllerBase
{
    private readonly ILogger<FileUploadController> _logger;
    private readonly IWebHostEnvironment _environment;
    private const string UploadBasePath = "uploads";
    private const long MaxFileSize = 100 * 1024 * 1024; // 100MB

    public FileUploadController(ILogger<FileUploadController> logger, IWebHostEnvironment environment)
    {
        _logger = logger;
        _environment = environment;
    }

    /// <summary>
    /// 上传文件
    /// </summary>
    [HttpPost]
    [RequestSizeLimit(MaxFileSize)]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<IActionResult> UploadFile(IFormFile file, [FromQuery] string? category = "knowledge")
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest(new { message = "文件不能为空" });
        }

        if (file.Length > MaxFileSize)
        {
            return BadRequest(new { message = $"文件大小不能超过 {MaxFileSize / 1024 / 1024}MB" });
        }

        try
        {
            // 确定文件类型和子目录
            var fileName = file.FileName;
            var extension = Path.GetExtension(fileName).ToLower();
            var subDirectory = GetSubDirectory(extension, category);
            
            // 生成唯一文件名
            var uniqueFileName = $"{Guid.NewGuid()}{extension}";
            // 使用 WebRootPath 以便静态文件服务可以访问
            var webRootPath = _environment.WebRootPath ?? Path.Combine(_environment.ContentRootPath, "wwwroot");
            var uploadPath = Path.Combine(webRootPath, UploadBasePath, subDirectory);
            
            // 确保目录存在
            if (!Directory.Exists(uploadPath))
            {
                Directory.CreateDirectory(uploadPath);
            }

            var filePath = Path.Combine(uploadPath, uniqueFileName);
            
            // 保存文件
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // 返回文件URL（相对于网站根路径）
            var fileUrl = $"/{UploadBasePath}/{subDirectory}/{uniqueFileName}";
            
            _logger.LogInformation($"文件上传成功: {fileUrl}");
            
            return Ok(new
            {
                url = fileUrl,
                name = fileName,
                size = file.Length,
                type = GetFileType(extension)
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "文件上传失败");
            return StatusCode(500, new { message = "文件上传失败" });
        }
    }

    /// <summary>
    /// 上传多个文件
    /// </summary>
    [HttpPost("multiple")]
    [RequestSizeLimit(MaxFileSize * 10)]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<IActionResult> UploadFiles(List<IFormFile> files, [FromQuery] string? category = "knowledge")
    {
        if (files == null || files.Count == 0)
        {
            return BadRequest(new { message = "文件不能为空" });
        }

        var results = new List<object>();

        foreach (var file in files)
        {
            if (file.Length == 0) continue;

            try
            {
                var fileName = file.FileName;
                var extension = Path.GetExtension(fileName).ToLower();
                var subDirectory = GetSubDirectory(extension, category);
                
                var uniqueFileName = $"{Guid.NewGuid()}{extension}";
                var webRootPath = _environment.WebRootPath ?? Path.Combine(_environment.ContentRootPath, "wwwroot");
                var uploadPath = Path.Combine(webRootPath, UploadBasePath, subDirectory);
                
                if (!Directory.Exists(uploadPath))
                {
                    Directory.CreateDirectory(uploadPath);
                }

                var filePath = Path.Combine(uploadPath, uniqueFileName);
                
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                var fileUrl = $"/{UploadBasePath}/{subDirectory}/{uniqueFileName}";
                
                results.Add(new
                {
                    url = fileUrl,
                    name = fileName,
                    size = file.Length,
                    type = GetFileType(extension)
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"文件 {file.FileName} 上传失败");
            }
        }

        return Ok(results);
    }

    /// <summary>
    /// 删除文件
    /// </summary>
    [HttpDelete]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public IActionResult DeleteFile([FromQuery] string url)
    {
        try
        {
            if (string.IsNullOrEmpty(url) || !url.StartsWith($"/{UploadBasePath}/"))
            {
                return BadRequest(new { message = "无效的文件路径" });
            }

            var webRootPath = _environment.WebRootPath ?? Path.Combine(_environment.ContentRootPath, "wwwroot");
            var filePath = Path.Combine(webRootPath, url.TrimStart('/'));
            
            if (System.IO.File.Exists(filePath))
            {
                System.IO.File.Delete(filePath);
                _logger.LogInformation($"文件删除成功: {url}");
                return NoContent();
            }

            return NotFound(new { message = "文件不存在" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "文件删除失败");
            return StatusCode(500, new { message = "文件删除失败" });
        }
    }

    private string GetSubDirectory(string extension, string category)
    {
        // 根据文件类型和分类确定子目录
        if (extension == ".pdf" || extension == ".doc" || extension == ".docx")
        {
            return Path.Combine(category, "documents");
        }
        else if (extension == ".jpg" || extension == ".jpeg" || extension == ".png" || 
                 extension == ".gif" || extension == ".bmp")
        {
            return Path.Combine(category, "images");
        }
        else if (extension == ".mp4" || extension == ".avi" || extension == ".mov" || 
                 extension == ".wmv" || extension == ".flv")
        {
            return Path.Combine(category, "videos");
        }
        else
        {
            return Path.Combine(category, "others");
        }
    }

    private string GetFileType(string extension)
    {
        if (extension == ".pdf") return "pdf";
        if (extension == ".doc" || extension == ".docx") return "word";
        if (extension == ".jpg" || extension == ".jpeg" || extension == ".png" || 
            extension == ".gif" || extension == ".bmp") return "image";
        if (extension == ".mp4" || extension == ".avi" || extension == ".mov" || 
            extension == ".wmv" || extension == ".flv") return "video";
        return "other";
    }
}

