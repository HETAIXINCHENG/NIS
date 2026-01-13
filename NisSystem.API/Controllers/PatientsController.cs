using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NisSystem.API.Data;
using NisSystem.API.DTOs;
using NisSystem.API.Helpers;
using NisSystem.API.Models;

namespace NisSystem.API.Controllers;

/// <summary>
/// 患者管理控制器
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PatientsController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<PatientsController> _logger;

    public PatientsController(ApplicationDbContext context, ILogger<PatientsController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// 获取患者列表（支持搜索、分页、筛选）
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<Patient>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPatients(
        [FromQuery] string? search,
        [FromQuery] string? department,
        [FromQuery] string? status,
        [FromQuery] string? nursingLevel,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        try
        {
            var query = _context.Patients
                .Include(p => p.AssignedNurse)
                .AsQueryable();

            // 搜索功能
            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(p =>
                    p.Name.Contains(search) ||
                    p.AdmissionNumber.Contains(search));
                // 注意：搜索加密字段可能性能较差，这里暂时移除
            }

            // 筛选
            if (!string.IsNullOrEmpty(department))
            {
                query = query.Where(p => p.Department == department);
            }

            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(p => p.Status == status);
            }

            if (!string.IsNullOrEmpty(nursingLevel))
            {
                query = query.Where(p => p.NursingLevel == nursingLevel);
            }

            // 分页
            var totalCount = await query.CountAsync();
            var patients = await query
                .OrderByDescending(p => p.AdmissionDate)
                .ThenByDescending(p => p.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // 解密敏感信息
            foreach (var patient in patients)
            {
                try
                {
                    if (!string.IsNullOrEmpty(patient.Diagnosis))
                        patient.Diagnosis = EncryptionHelper.Decrypt(patient.Diagnosis);
                    if (!string.IsNullOrEmpty(patient.Allergies))
                        patient.Allergies = EncryptionHelper.Decrypt(patient.Allergies);
                    if (!string.IsNullOrEmpty(patient.MedicationHistory))
                        patient.MedicationHistory = EncryptionHelper.Decrypt(patient.MedicationHistory);
                    if (!string.IsNullOrEmpty(patient.ContactInfo))
                        patient.ContactInfo = EncryptionHelper.Decrypt(patient.ContactInfo);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "解密患者 {PatientId} 的敏感信息失败", patient.Id);
                    // 解密失败时保留原值
                }
            }

            var result = new PagedResult<Patient>
            {
                Items = patients,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
            };

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取患者列表失败");
            return StatusCode(500, new { message = "获取患者列表失败", error = ex.Message });
        }
    }

    /// <summary>
    /// 根据ID获取患者信息
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(Patient), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPatient(Guid id)
    {
        var patient = await _context.Patients
            .Include(p => p.AssignedNurse)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (patient == null)
        {
            return NotFound();
        }

        // 解密敏感信息
        if (!string.IsNullOrEmpty(patient.Diagnosis))
            patient.Diagnosis = EncryptionHelper.Decrypt(patient.Diagnosis);
        if (!string.IsNullOrEmpty(patient.Allergies))
            patient.Allergies = EncryptionHelper.Decrypt(patient.Allergies);
        if (!string.IsNullOrEmpty(patient.MedicationHistory))
            patient.MedicationHistory = EncryptionHelper.Decrypt(patient.MedicationHistory);
        if (!string.IsNullOrEmpty(patient.ContactInfo))
            patient.ContactInfo = EncryptionHelper.Decrypt(patient.ContactInfo);

        return Ok(patient);
    }

    /// <summary>
    /// 创建患者
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Admin,Nurse,HeadNurse")]
    [ProducesResponseType(typeof(Patient), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreatePatient([FromBody] Patient patient)
    {
        // 加密敏感信息
        if (!string.IsNullOrEmpty(patient.Diagnosis))
            patient.Diagnosis = EncryptionHelper.Encrypt(patient.Diagnosis);
        if (!string.IsNullOrEmpty(patient.Allergies))
            patient.Allergies = EncryptionHelper.Encrypt(patient.Allergies);
        if (!string.IsNullOrEmpty(patient.MedicationHistory))
            patient.MedicationHistory = EncryptionHelper.Encrypt(patient.MedicationHistory);
        if (!string.IsNullOrEmpty(patient.ContactInfo))
            patient.ContactInfo = EncryptionHelper.Encrypt(patient.ContactInfo);

        patient.Id = Guid.NewGuid();
        patient.CreatedAt = DateTime.UtcNow;
        patient.CreatedBy = User.Identity?.Name;

        _context.Patients.Add(patient);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetPatient), new { id = patient.Id }, patient);
    }

    /// <summary>
    /// 更新患者信息
    /// </summary>
    [HttpPut("{id}")]
    [Authorize(Roles = "Admin,Nurse,HeadNurse")]
    [ProducesResponseType(typeof(Patient), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdatePatient(Guid id, [FromBody] Patient patient)
    {
        var existingPatient = await _context.Patients.FindAsync(id);
        if (existingPatient == null)
        {
            return NotFound();
        }

        // 更新字段
        existingPatient.Name = patient.Name;
        existingPatient.Gender = patient.Gender;
        existingPatient.Age = patient.Age;
        existingPatient.Diagnosis = !string.IsNullOrEmpty(patient.Diagnosis) 
            ? EncryptionHelper.Encrypt(patient.Diagnosis) 
            : existingPatient.Diagnosis;
        existingPatient.Allergies = !string.IsNullOrEmpty(patient.Allergies) 
            ? EncryptionHelper.Encrypt(patient.Allergies) 
            : existingPatient.Allergies;
        existingPatient.MedicationHistory = !string.IsNullOrEmpty(patient.MedicationHistory) 
            ? EncryptionHelper.Encrypt(patient.MedicationHistory) 
            : existingPatient.MedicationHistory;
        existingPatient.ContactInfo = !string.IsNullOrEmpty(patient.ContactInfo) 
            ? EncryptionHelper.Encrypt(patient.ContactInfo) 
            : existingPatient.ContactInfo;
        existingPatient.NursingLevel = patient.NursingLevel;
        existingPatient.Department = patient.Department;
        existingPatient.RoomNumber = patient.RoomNumber;
        existingPatient.BedNumber = patient.BedNumber;
        existingPatient.Status = patient.Status;
        existingPatient.UpdatedAt = DateTime.UtcNow;
        existingPatient.UpdatedBy = User.Identity?.Name;

        await _context.SaveChangesAsync();

        return Ok(existingPatient);
    }

    /// <summary>
    /// 删除患者（软删除）
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin,HeadNurse")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeletePatient(Guid id)
    {
        var patient = await _context.Patients.FindAsync(id);
        if (patient == null)
        {
            return NotFound();
        }

        patient.IsDeleted = true;
        patient.UpdatedAt = DateTime.UtcNow;
        patient.UpdatedBy = User.Identity?.Name;

        await _context.SaveChangesAsync();

        return NoContent();
    }
}

