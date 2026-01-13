using Microsoft.EntityFrameworkCore;
using NisSystem.API.Models;

namespace NisSystem.API.Data;

/// <summary>
/// 应用程序数据库上下文
/// </summary>
public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    // DbSets
    public DbSet<User> Users { get; set; }
    public DbSet<Role> Roles { get; set; }
    public DbSet<Patient> Patients { get; set; }
    public DbSet<VitalSigns> VitalSigns { get; set; }
    public DbSet<NursingAssessment> NursingAssessments { get; set; }
    public DbSet<NursingRecord> NursingRecords { get; set; }
    public DbSet<MedicationOrder> MedicationOrders { get; set; }
    public DbSet<MedicationExecution> MedicationExecutions { get; set; }
    public DbSet<MedicationPreparation> MedicationPreparations { get; set; }
    public DbSet<MedicationEffect> MedicationEffects { get; set; }
    public DbSet<ConditionObservation> ConditionObservations { get; set; }
    public DbSet<KnowledgeBase> KnowledgeBases { get; set; }
    public DbSet<SystemIntegration> SystemIntegrations { get; set; }
    public DbSet<NursingPlan> NursingPlans { get; set; }
    public DbSet<ShiftHandover> ShiftHandovers { get; set; }
    public DbSet<Schedule> Schedules { get; set; }
    public DbSet<AuditLog> AuditLogs { get; set; }
    public DbSet<Department> Departments { get; set; }
    public DbSet<SystemSettings> SystemSettings { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // 配置实体关系
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(e => e.Username).IsUnique();
            entity.HasIndex(e => e.EmployeeId).IsUnique();
            entity.HasOne(e => e.Role)
                .WithMany(r => r.Users)
                .HasForeignKey(e => e.RoleId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.DepartmentNavigation)
                .WithMany()
                .HasForeignKey(e => e.DepartmentId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<Patient>(entity =>
        {
            entity.HasIndex(e => e.AdmissionNumber).IsUnique();
            entity.HasOne(e => e.AssignedNurse)
                .WithMany(u => u.Patients)
                .HasForeignKey(e => e.AssignedNurseId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<VitalSigns>(entity =>
        {
            entity.HasOne(e => e.Patient)
                .WithMany(p => p.VitalSigns)
                .HasForeignKey(e => e.PatientId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.RecordedBy)
                .WithMany()
                .HasForeignKey(e => e.RecordedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<NursingAssessment>(entity =>
        {
            entity.HasOne(e => e.Patient)
                .WithMany(p => p.NursingAssessments)
                .HasForeignKey(e => e.PatientId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.AssessedBy)
                .WithMany()
                .HasForeignKey(e => e.AssessedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<NursingRecord>(entity =>
        {
            entity.HasOne(e => e.Patient)
                .WithMany(p => p.NursingRecords)
                .HasForeignKey(e => e.PatientId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.RecordedBy)
                .WithMany(u => u.NursingRecords)
                .HasForeignKey(e => e.RecordedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<MedicationOrder>(entity =>
        {
            entity.HasIndex(e => e.OrderNumber).IsUnique();
            entity.HasOne(e => e.Patient)
                .WithMany(p => p.MedicationOrders)
                .HasForeignKey(e => e.PatientId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<MedicationExecution>(entity =>
        {
            entity.HasOne(e => e.MedicationOrder)
                .WithMany(o => o.Executions)
                .HasForeignKey(e => e.MedicationOrderId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.ExecutedBy)
                .WithMany(u => u.MedicationExecutions)
                .HasForeignKey(e => e.ExecutedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.VerifiedBy)
                .WithMany()
                .HasForeignKey(e => e.VerifiedByUserId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<NursingPlan>(entity =>
        {
            entity.HasOne(e => e.Patient)
                .WithMany(p => p.NursingPlans)
                .HasForeignKey(e => e.PatientId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.CreatedByUser)
                .WithMany()
                .HasForeignKey(e => e.CreatedByUserId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<ShiftHandover>(entity =>
        {
            entity.HasOne(e => e.HandoverFrom)
                .WithMany(u => u.ShiftHandovers)
                .HasForeignKey(e => e.HandoverFromUserId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.HandoverTo)
                .WithMany()
                .HasForeignKey(e => e.HandoverToUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Schedule>(entity =>
        {
            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            // 确保 WorkDate 存储为 UTC
            entity.Property(e => e.WorkDate)
                .HasConversion(
                    v => v.Kind == DateTimeKind.Unspecified ? DateTime.SpecifyKind(v, DateTimeKind.Utc) : v.ToUniversalTime(),
                    v => v.ToUniversalTime());
        });

        modelBuilder.Entity<MedicationPreparation>(entity =>
        {
            entity.HasOne(e => e.MedicationOrder)
                .WithMany()
                .HasForeignKey(e => e.MedicationOrderId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.PreparedBy)
                .WithMany()
                .HasForeignKey(e => e.PreparedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<MedicationEffect>(entity =>
        {
            entity.HasOne(e => e.MedicationExecution)
                .WithMany()
                .HasForeignKey(e => e.MedicationExecutionId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.ObservedBy)
                .WithMany()
                .HasForeignKey(e => e.ObservedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ConditionObservation>(entity =>
        {
            entity.HasOne(e => e.Patient)
                .WithMany()
                .HasForeignKey(e => e.PatientId)
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired(); // 明确指定为必需关系
            entity.HasOne(e => e.ObservedBy)
                .WithMany()
                .HasForeignKey(e => e.ObservedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<MedicationPreparation>(entity =>
        {
            entity.HasOne(e => e.MedicationOrder)
                .WithMany()
                .HasForeignKey(e => e.MedicationOrderId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.PreparedBy)
                .WithMany()
                .HasForeignKey(e => e.PreparedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.HasOne(e => e.User)
                .WithMany(u => u.AuditLogs)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // 软删除查询过滤器
        modelBuilder.Entity<User>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<Role>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<Patient>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<VitalSigns>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<NursingAssessment>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<NursingRecord>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<MedicationOrder>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<MedicationExecution>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<MedicationPreparation>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<MedicationEffect>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<ConditionObservation>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<KnowledgeBase>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<SystemIntegration>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<NursingPlan>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<ShiftHandover>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<Schedule>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<Department>().HasQueryFilter(e => !e.IsDeleted);

        // 配置 Department 实体
        modelBuilder.Entity<Department>(entity =>
        {
            entity.HasIndex(e => e.Name).IsUnique();
            entity.HasIndex(e => e.Code).IsUnique().HasFilter("\"Code\" IS NOT NULL");
        });
    }
}

