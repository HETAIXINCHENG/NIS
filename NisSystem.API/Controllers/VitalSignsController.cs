using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NisSystem.API.Data;
using NisSystem.API.Models;
using NisSystem.API.DTOs;
using System.Security.Claims;
using System.Linq;

namespace NisSystem.API.Controllers;

/// <summary>
/// 生命体征记录控制器
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class VitalSignsController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<VitalSignsController> _logger;

    public VitalSignsController(ApplicationDbContext context, ILogger<VitalSignsController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// 获取患者生命体征记录
    /// </summary>
    [HttpGet("patient/{patientId}")]
    [ProducesResponseType(typeof(List<VitalSigns>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetVitalSignsByPatient(Guid patientId, [FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate)
    {
        var query = _context.VitalSigns
            .Include(v => v.RecordedBy)
            .Where(v => v.PatientId == patientId)
            .AsQueryable();

        if (startDate.HasValue)
        {
            query = query.Where(v => v.RecordedAt >= startDate.Value);
        }

        if (endDate.HasValue)
        {
            query = query.Where(v => v.RecordedAt <= endDate.Value);
        }

        var vitalSigns = await query
            .OrderByDescending(v => v.RecordedAt)
            .ToListAsync();

        // 清除循环引用，避免序列化错误
        foreach (var vs in vitalSigns)
        {
            if (vs.Patient != null)
            {
                vs.Patient.VitalSigns = new List<VitalSigns>();
            }
        }

        return Ok(vitalSigns);
    }

    /// <summary>
    /// 创建生命体征记录
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Admin,Nurse,HeadNurse")]
    [ProducesResponseType(typeof(VitalSigns), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateVitalSigns([FromBody] CreateVitalSignsDto dto)
    {
        try
        {
            _logger.LogInformation("CreateVitalSigns: 收到创建请求");
            
            if (dto == null)
            {
                _logger.LogWarning("CreateVitalSigns: 请求体为空");
                return BadRequest(new { message = "请求数据不能为空" });
            }

            _logger.LogInformation("CreateVitalSigns: 接收到的数据 - PatientId: {PatientId}, RecordedAt: {RecordedAt}, Temperature: {Temperature}, Pulse: {Pulse}, Respiration: {Respiration}, SystolicBP: {SystolicBP}, DiastolicBP: {DiastolicBP}, OxygenSaturation: {OxygenSaturation}", 
                dto.PatientId, dto.RecordedAt, dto.Temperature, dto.Pulse, dto.Respiration, dto.SystolicBP, dto.DiastolicBP, dto.OxygenSaturation);

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("CreateVitalSigns: 模型验证失败");
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                _logger.LogWarning("CreateVitalSigns: 验证错误: {Errors}", string.Join(", ", errors));
                return BadRequest(new { message = "数据验证失败", errors = errors });
            }

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                _logger.LogWarning("CreateVitalSigns: 用户ID无效");
                return Unauthorized();
            }

            // 验证患者是否存在
            if (dto.PatientId == Guid.Empty)
            {
                _logger.LogWarning("CreateVitalSigns: 患者ID为空");
                return BadRequest(new { message = "患者ID不能为空" });
            }

            var patient = await _context.Patients.FindAsync(dto.PatientId);
            if (patient == null)
            {
                _logger.LogWarning("CreateVitalSigns: 患者不存在, PatientId: {PatientId}", dto.PatientId);
                return BadRequest(new { message = "患者不存在" });
            }

            // 创建 VitalSigns 实体
            var vitalSigns = new VitalSigns
            {
                Id = Guid.NewGuid(),
                PatientId = dto.PatientId,
                RecordedByUserId = userId,
                RecordedAt = dto.RecordedAt == default(DateTime) ? DateTime.UtcNow : dto.RecordedAt,
                Temperature = dto.Temperature,
                Pulse = dto.Pulse,
                Respiration = dto.Respiration,
                SystolicBP = dto.SystolicBP,
                DiastolicBP = dto.DiastolicBP,
                OxygenSaturation = dto.OxygenSaturation,
                BloodGlucose = dto.BloodGlucose,
                Notes = dto.Notes,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = User.Identity?.Name
            };

            // 检查异常值
            vitalSigns.IsAbnormal = CheckAbnormalValues(vitalSigns);

            _context.VitalSigns.Add(vitalSigns);
            await _context.SaveChangesAsync();

            // 重新加载数据，但不加载导航属性以避免循环引用
            await _context.Entry(vitalSigns).Reference(v => v.RecordedBy).LoadAsync();
            
            // 清除 Patient 导航属性以避免循环引用
            await _context.Entry(vitalSigns).Reference(v => v.Patient).LoadAsync();
            if (vitalSigns.Patient != null)
            {
                vitalSigns.Patient.VitalSigns = new List<VitalSigns>(); // 清空循环引用
            }

            _logger.LogInformation("CreateVitalSigns: 成功创建生命体征记录, Id: {Id}, PatientId: {PatientId}", vitalSigns.Id, vitalSigns.PatientId);
            return CreatedAtAction(nameof(GetVitalSignsByPatient), new { patientId = vitalSigns.PatientId }, vitalSigns);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CreateVitalSigns: 创建生命体征记录时发生错误");
            return StatusCode(500, new { message = "创建生命体征记录失败", error = ex.Message });
        }
    }

    /// <summary>
    /// 检查生命体征是否异常
    /// </summary>
    private bool CheckAbnormalValues(VitalSigns vitalSigns)
    {
        // 体温异常：< 36℃ 或 > 37.5℃
        if (vitalSigns.Temperature.HasValue && (vitalSigns.Temperature < 36 || vitalSigns.Temperature > 37.5m))
            return true;

        // 脉搏异常：< 60 或 > 100
        if (vitalSigns.Pulse.HasValue && (vitalSigns.Pulse < 60 || vitalSigns.Pulse > 100))
            return true;

        // 呼吸异常：< 12 或 > 20
        if (vitalSigns.Respiration.HasValue && (vitalSigns.Respiration < 12 || vitalSigns.Respiration > 20))
            return true;

        // 血压异常：收缩压 < 90 或 > 140，舒张压 < 60 或 > 90
        if (vitalSigns.SystolicBP.HasValue && (vitalSigns.SystolicBP < 90 || vitalSigns.SystolicBP > 140))
            return true;
        if (vitalSigns.DiastolicBP.HasValue && (vitalSigns.DiastolicBP < 60 || vitalSigns.DiastolicBP > 90))
            return true;

        // 血氧饱和度异常：< 95%
        if (vitalSigns.OxygenSaturation.HasValue && vitalSigns.OxygenSaturation < 95)
            return true;

        // 血糖异常：< 3.9 或 > 6.1 mmol/L
        if (vitalSigns.BloodGlucose.HasValue && (vitalSigns.BloodGlucose < 3.9m || vitalSigns.BloodGlucose > 6.1m))
            return true;

        return false;
    }

    /// <summary>
    /// 更新生命体征记录
    /// </summary>
    [HttpPut("{id}")]
    [Authorize(Roles = "Admin,Nurse,HeadNurse")]
    [ProducesResponseType(typeof(VitalSigns), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateVitalSigns(Guid id, [FromBody] UpdateVitalSignsDto dto)
    {
        try
        {
            _logger.LogInformation("UpdateVitalSigns: 收到更新请求, ID: {Id}", id);

            if (dto == null)
            {
                _logger.LogWarning("UpdateVitalSigns: 请求体为空");
                return BadRequest(new { message = "请求数据不能为空" });
            }

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("UpdateVitalSigns: 模型验证失败");
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                return BadRequest(new { message = "数据验证失败", errors = errors });
            }

            var existing = await _context.VitalSigns.FindAsync(id);
            if (existing == null)
            {
                _logger.LogWarning("UpdateVitalSigns: 记录不存在, ID: {Id}", id);
                return NotFound();
            }

            // 更新字段
            existing.Temperature = dto.Temperature;
            existing.Pulse = dto.Pulse;
            existing.Respiration = dto.Respiration;
            existing.SystolicBP = dto.SystolicBP;
            existing.DiastolicBP = dto.DiastolicBP;
            existing.OxygenSaturation = dto.OxygenSaturation;
            existing.BloodGlucose = dto.BloodGlucose;
            existing.RecordedAt = dto.RecordedAt == default(DateTime) ? DateTime.UtcNow : dto.RecordedAt;
            existing.Notes = dto.Notes;
            existing.IsAbnormal = CheckAbnormalValues(existing);
            existing.UpdatedAt = DateTime.UtcNow;
            existing.UpdatedBy = User.Identity?.Name;

            await _context.SaveChangesAsync();

            // 清除导航属性以避免循环引用
            await _context.Entry(existing).Reference(v => v.RecordedBy).LoadAsync();
            if (existing.Patient != null)
            {
                existing.Patient.VitalSigns = new List<VitalSigns>();
            }

            _logger.LogInformation("UpdateVitalSigns: 成功更新生命体征记录, ID: {Id}", id);
            return Ok(existing);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "UpdateVitalSigns: 更新生命体征记录时发生错误, ID: {Id}", id);
            return StatusCode(500, new { message = "更新生命体征记录失败", error = ex.Message });
        }
    }

    /// <summary>
    /// 删除生命体征记录
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin,HeadNurse")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteVitalSigns(Guid id)
    {
        var vitalSigns = await _context.VitalSigns.FindAsync(id);
        if (vitalSigns == null)
        {
            return NotFound();
        }

        _context.VitalSigns.Remove(vitalSigns);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}

