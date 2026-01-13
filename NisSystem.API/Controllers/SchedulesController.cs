using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NisSystem.API.Data;
using NisSystem.API.Models;
using System.Security.Claims;

namespace NisSystem.API.Controllers;

/// <summary>
/// 排班管理控制器
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SchedulesController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<SchedulesController> _logger;

    public SchedulesController(ApplicationDbContext context, ILogger<SchedulesController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// 获取排班列表
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<Schedule>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSchedules(
        [FromQuery] Guid? userId,
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate,
        [FromQuery] string? department)
    {
        var query = _context.Schedules
            .Include(s => s.User)
            .AsQueryable();

        if (userId.HasValue)
        {
            query = query.Where(s => s.UserId == userId.Value);
        }

        if (startDate.HasValue)
        {
            query = query.Where(s => s.WorkDate >= startDate.Value);
        }

        if (endDate.HasValue)
        {
            query = query.Where(s => s.WorkDate <= endDate.Value);
        }

        if (!string.IsNullOrEmpty(department))
        {
            query = query.Where(s => s.Department == department);
        }

        var schedules = await query
            .OrderBy(s => s.WorkDate)
            .ThenBy(s => s.StartTime)
            .ToListAsync();

        return Ok(schedules);
    }

    /// <summary>
    /// 创建排班
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Admin,HeadNurse")]
    [ProducesResponseType(typeof(Schedule), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateSchedule([FromBody] CreateScheduleDto dto)
    {
        var user = await _context.Users.FindAsync(dto.UserId);
        if (user == null)
        {
            return NotFound(new { message = "用户不存在" });
        }

        // 确保 WorkDate 是 UTC
        var workDate = dto.WorkDate.Kind == DateTimeKind.Unspecified
            ? DateTime.SpecifyKind(dto.WorkDate, DateTimeKind.Utc)
            : dto.WorkDate.ToUniversalTime();

        var schedule = new Schedule
        {
            Id = Guid.NewGuid(),
            UserId = dto.UserId,
            WorkDate = workDate.Date,
            ShiftType = dto.ShiftType,
            StartTime = dto.StartTime,
            EndTime = dto.EndTime,
            Department = dto.Department ?? user.Department,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = User.Identity?.Name
        };

        // 计算实际工作时间
        if (dto.StartTime.HasValue && dto.EndTime.HasValue)
        {
            var start = workDate.Date.Add(dto.StartTime.Value);
            var end = workDate.Date.Add(dto.EndTime.Value);
            if (end < start)
            {
                end = end.AddDays(1); // 跨天
            }
            schedule.ActualWorkHours = (decimal)(end - start).TotalHours;
        }

        _context.Schedules.Add(schedule);
        await _context.SaveChangesAsync();

        await _context.Entry(schedule).Reference(s => s.User).LoadAsync();

        return CreatedAtAction(nameof(GetSchedules), new { id = schedule.Id }, schedule);
    }

    /// <summary>
    /// 批量创建排班
    /// </summary>
    [HttpPost("batch")]
    [Authorize(Roles = "Admin,HeadNurse")]
    [ProducesResponseType(typeof(List<Schedule>), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateBatchSchedules([FromBody] List<CreateScheduleDto> dtos)
    {
        var schedules = new List<Schedule>();

        foreach (var dto in dtos)
        {
            var user = await _context.Users.FindAsync(dto.UserId);
            if (user == null)
            {
                continue;
            }

            // 确保 WorkDate 是 UTC
            var workDate = dto.WorkDate.Kind == DateTimeKind.Unspecified
                ? DateTime.SpecifyKind(dto.WorkDate, DateTimeKind.Utc)
                : dto.WorkDate.ToUniversalTime();

            var schedule = new Schedule
            {
                Id = Guid.NewGuid(),
                UserId = dto.UserId,
                WorkDate = workDate.Date,
                ShiftType = dto.ShiftType,
                StartTime = dto.StartTime,
                EndTime = dto.EndTime,
                Department = dto.Department ?? user.Department,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = User.Identity?.Name
            };

            if (dto.StartTime.HasValue && dto.EndTime.HasValue)
            {
                var start = workDate.Date.Add(dto.StartTime.Value);
                var end = workDate.Date.Add(dto.EndTime.Value);
                if (end < start)
                {
                    end = end.AddDays(1);
                }
                schedule.ActualWorkHours = (decimal)(end - start).TotalHours;
            }

            schedules.Add(schedule);
        }

        _context.Schedules.AddRange(schedules);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetSchedules), schedules);
    }

    /// <summary>
    /// 自动排班（简化算法）
    /// </summary>
    [HttpPost("auto-schedule")]
    [Authorize(Roles = "Admin,HeadNurse")]
    [ProducesResponseType(typeof(List<Schedule>), StatusCodes.Status201Created)]
    public async Task<IActionResult> AutoSchedule([FromBody] AutoScheduleDto dto)
    {
        // 获取指定科室的所有护士
        var nurses = await _context.Users
            .Where(u => u.Department == dto.Department &&
                       u.IsActive &&
                       !u.IsDeleted &&
                       (u.Position == "护士" || u.Position == "护师"))
            .ToListAsync();

        if (nurses.Count == 0)
        {
            return BadRequest(new { message = "该科室没有可用的护士" });
        }

        var schedules = new List<Schedule>();
        // 确保日期是 UTC
        var startDate = dto.StartDate.Kind == DateTimeKind.Unspecified
            ? DateTime.SpecifyKind(dto.StartDate, DateTimeKind.Utc)
            : dto.StartDate.ToUniversalTime();
        var endDate = dto.EndDate.Kind == DateTimeKind.Unspecified
            ? DateTime.SpecifyKind(dto.EndDate, DateTimeKind.Utc)
            : dto.EndDate.ToUniversalTime();
        var currentDate = startDate;
        var nurseIndex = 0;

        // 简单的轮班算法
        while (currentDate <= endDate)
        {
            var nurse = nurses[nurseIndex % nurses.Count];
            
            // 白班
            schedules.Add(new Schedule
            {
                Id = Guid.NewGuid(),
                UserId = nurse.Id,
                WorkDate = currentDate.Date,
                ShiftType = "白班",
                StartTime = TimeSpan.FromHours(8),
                EndTime = TimeSpan.FromHours(16),
                Department = dto.Department,
                ActualWorkHours = 8,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = User.Identity?.Name
            });

            // 夜班（下一个护士）
            var nightNurse = nurses[(nurseIndex + 1) % nurses.Count];
            schedules.Add(new Schedule
            {
                Id = Guid.NewGuid(),
                UserId = nightNurse.Id,
                WorkDate = currentDate.Date,
                ShiftType = "夜班",
                StartTime = TimeSpan.FromHours(20),
                EndTime = TimeSpan.FromHours(8).Add(TimeSpan.FromDays(1)),
                Department = dto.Department,
                ActualWorkHours = 12,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = User.Identity?.Name
            });

            nurseIndex += 2;
            currentDate = currentDate.AddDays(1);
        }

        _context.Schedules.AddRange(schedules);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetSchedules), schedules);
    }

    /// <summary>
    /// 获取工时统计
    /// </summary>
    [HttpGet("work-hours")]
    [Authorize(Roles = "Admin,HeadNurse")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetWorkHoursStatistics(
        [FromQuery] Guid? userId,
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate)
    {
        var query = _context.Schedules.AsQueryable();

        if (userId.HasValue)
        {
            query = query.Where(s => s.UserId == userId.Value);
        }

        if (startDate.HasValue)
        {
            var utcStartDate = startDate.Value.Kind == DateTimeKind.Unspecified
                ? DateTime.SpecifyKind(startDate.Value, DateTimeKind.Utc)
                : startDate.Value.ToUniversalTime();
            query = query.Where(s => s.WorkDate >= utcStartDate);
        }

        if (endDate.HasValue)
        {
            var utcEndDate = endDate.Value.Kind == DateTimeKind.Unspecified
                ? DateTime.SpecifyKind(endDate.Value, DateTimeKind.Utc)
                : endDate.Value.ToUniversalTime();
            query = query.Where(s => s.WorkDate <= utcEndDate);
        }

        var schedules = await query.ToListAsync();

        var statistics = schedules
            .GroupBy(s => s.UserId)
            .Select(g => new
            {
                UserId = g.Key,
                TotalWorkHours = g.Sum(s => s.ActualWorkHours ?? 0),
                OvertimeHours = g.Where(s => s.IsOvertime).Sum(s => s.ActualWorkHours ?? 0),
                WorkDays = g.Count(),
                OvertimeDays = g.Count(s => s.IsOvertime)
            })
            .ToList();

        return Ok(statistics);
    }

    /// <summary>
    /// 获取护士列表（用于排班）
    /// </summary>
    [HttpGet("nurses")]
    [ProducesResponseType(typeof(List<User>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetNurses([FromQuery] string? department)
    {
        var query = _context.Users
            .Include(u => u.DepartmentNavigation)
            .Where(u => u.IsActive && !u.IsDeleted &&
                       (u.Position == "护士" || u.Position == "护师" || u.Position == "护长" ||
                        u.Position == "主管护师" || u.Position == "副主任护师" || u.Position == "主任护师"))
            .AsQueryable();

        if (!string.IsNullOrEmpty(department) && department != "全部")
        {
            // 支持通过科室名称或ID筛选
            // 先尝试通过名称匹配（兼容旧数据）
            query = query.Where(u => 
                (u.DepartmentNavigation != null && u.DepartmentNavigation.Name == department) ||
                (u.Department != null && u.Department == department));
        }

        var nurses = await query
            .OrderBy(u => u.DepartmentNavigation != null ? u.DepartmentNavigation.Name : "")
            .ThenBy(u => u.Name)
            .ToListAsync();

        return Ok(nurses);
    }

    /// <summary>
    /// 获取排班统计（按周统计每个护士的班次数量）
    /// </summary>
    [HttpGet("statistics")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetScheduleStatistics(
        [FromQuery] DateTime startDate,
        [FromQuery] DateTime endDate,
        [FromQuery] string? department)
    {
        // 确保日期是 UTC
        var utcStartDate = startDate.Kind == DateTimeKind.Unspecified
            ? DateTime.SpecifyKind(startDate, DateTimeKind.Utc)
            : startDate.ToUniversalTime();
        var utcEndDate = endDate.Kind == DateTimeKind.Unspecified
            ? DateTime.SpecifyKind(endDate, DateTimeKind.Utc)
            : endDate.ToUniversalTime();

        var query = _context.Schedules
            .Include(s => s.User)
            .Where(s => s.WorkDate >= utcStartDate && s.WorkDate <= utcEndDate)
            .AsQueryable();

        if (!string.IsNullOrEmpty(department))
        {
            query = query.Where(s => s.Department == department);
        }

        var schedules = await query.ToListAsync();

        var statistics = schedules
            .GroupBy(s => s.UserId)
            .Select(g => new
            {
                UserId = g.Key,
                UserName = g.First().User.Name,
                DayShift = g.Count(s => s.ShiftType == "白班"),
                NightShift = g.Count(s => s.ShiftType == "夜班"),
                Rest = g.Count(s => s.ShiftType == "休息"),
                Treatment = g.Count(s => s.ShiftType == "治疗")
            })
            .ToList();

        return Ok(statistics);
    }

    /// <summary>
    /// 更新排班
    /// </summary>
    [HttpPut("{id}")]
    [Authorize(Roles = "Admin,HeadNurse")]
    [ProducesResponseType(typeof(Schedule), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateSchedule(Guid id, [FromBody] UpdateScheduleDto dto)
    {
        var schedule = await _context.Schedules
            .Include(s => s.User)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (schedule == null)
        {
            return NotFound(new { message = "排班记录不存在" });
        }

        if (dto.WorkDate.HasValue)
        {
            schedule.WorkDate = dto.WorkDate.Value;
        }

        if (!string.IsNullOrEmpty(dto.ShiftType))
        {
            schedule.ShiftType = dto.ShiftType;
        }

        if (dto.StartTime.HasValue)
        {
            schedule.StartTime = dto.StartTime;
        }

        if (dto.EndTime.HasValue)
        {
            schedule.EndTime = dto.EndTime;
        }

        // 重新计算实际工作时间
        if (schedule.StartTime.HasValue && schedule.EndTime.HasValue)
        {
            var start = schedule.WorkDate.Date.Add(schedule.StartTime.Value);
            var end = schedule.WorkDate.Date.Add(schedule.EndTime.Value);
            if (end < start)
            {
                end = end.AddDays(1);
            }
            schedule.ActualWorkHours = (decimal)(end - start).TotalHours;
        }

        schedule.UpdatedAt = DateTime.UtcNow;
        schedule.UpdatedBy = User.Identity?.Name;

        await _context.SaveChangesAsync();

        await _context.Entry(schedule).Reference(s => s.User).LoadAsync();

        return Ok(schedule);
    }

    /// <summary>
    /// 删除排班
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin,HeadNurse")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteSchedule(Guid id)
    {
        var schedule = await _context.Schedules.FindAsync(id);
        if (schedule == null)
        {
            return NotFound(new { message = "排班记录不存在" });
        }

        _context.Schedules.Remove(schedule);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}

/// <summary>
/// 创建排班DTO
/// </summary>
public class CreateScheduleDto
{
    public Guid UserId { get; set; }
    public DateTime WorkDate { get; set; }
    public string ShiftType { get; set; } = string.Empty;
    public TimeSpan? StartTime { get; set; }
    public TimeSpan? EndTime { get; set; }
    public string? Department { get; set; }
}

/// <summary>
/// 自动排班DTO
/// </summary>
public class AutoScheduleDto
{
    public string Department { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
}

/// <summary>
/// 更新排班DTO
/// </summary>
public class UpdateScheduleDto
{
    public DateTime? WorkDate { get; set; }
    public string? ShiftType { get; set; }
    public TimeSpan? StartTime { get; set; }
    public TimeSpan? EndTime { get; set; }
}

