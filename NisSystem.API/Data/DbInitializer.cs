using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage;
using NisSystem.API.Models;
using NisSystem.API.Helpers;
using BCrypt.Net;
using Npgsql;
using System.Data;
using System.Linq;

namespace NisSystem.API.Data;

/// <summary>
/// 数据库初始化器
/// </summary>
public static class DbInitializer
{
    /// <summary>
    /// 初始化数据库（创建初始角色和用户）
    /// </summary>
    public static async Task InitializeAsync(ApplicationDbContext context, ILogger logger)
    {
        try
        {
            // 检查数据库连接
            if (!context.Database.CanConnect())
            {
                logger.LogWarning("无法连接到数据库");
                return;
            }

            // 先检查关键表是否已存在
            bool tablesExist = false;
            try
            {
                var connection = context.Database.GetDbConnection();
                if (connection.State != System.Data.ConnectionState.Open)
                {
                    await connection.OpenAsync();
                }

                using var checkTableCommand = connection.CreateCommand();
                checkTableCommand.CommandText = @"
                    SELECT EXISTS (
                        SELECT FROM information_schema.tables 
                        WHERE table_schema = 'public' 
                        AND table_name IN ('Roles', 'Users', 'Patients', 'Departments')
                    );";
                var tableResult = await checkTableCommand.ExecuteScalarAsync();
                tablesExist = tableResult != null && Convert.ToBoolean(tableResult);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "检查表是否存在时出错，假设不存在");
                tablesExist = false;
            }

            // 检查 __EFMigrationsHistory 表是否存在
            bool migrationHistoryTableExists = false;
            try
            {
                var connection = context.Database.GetDbConnection();
                if (connection.State != System.Data.ConnectionState.Open)
                {
                    await connection.OpenAsync();
                }

                using var checkCommand = connection.CreateCommand();
                checkCommand.CommandText = @"
                    SELECT EXISTS (
                        SELECT FROM information_schema.tables 
                        WHERE table_schema = 'public' 
                        AND table_name = '__EFMigrationsHistory'
                    );";
                var result = await checkCommand.ExecuteScalarAsync();
                migrationHistoryTableExists = result != null && Convert.ToBoolean(result);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "检查迁移历史表时出错，假设不存在");
                migrationHistoryTableExists = false;
            }

            // 如果迁移历史表存在但关键表不存在，说明数据库状态不一致
            // 需要清空迁移历史表并重新应用迁移
            if (migrationHistoryTableExists && !tablesExist)
            {
                logger.LogWarning("检测到迁移历史表存在但关键表不存在，清空迁移历史表并重新应用迁移...");
                try
                {
                    var connection = context.Database.GetDbConnection();
                    if (connection.State != System.Data.ConnectionState.Open)
                    {
                        await connection.OpenAsync();
                    }

                    using var clearCommand = connection.CreateCommand();
                    clearCommand.CommandText = @"DELETE FROM ""__EFMigrationsHistory"";";
                    await clearCommand.ExecuteNonQueryAsync();
                    logger.LogInformation("迁移历史表已清空");
                    migrationHistoryTableExists = false; // 标记为不存在，以便后续执行迁移
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "清空迁移历史表失败: {Message}", ex.Message);
                }
            }

            // 应用数据库迁移
            var pendingMigrations = context.Database.GetPendingMigrations().ToList();
            if (pendingMigrations.Any() || !tablesExist)
            {
                // 如果有待应用的迁移，或者表不存在，执行迁移
                if (!tablesExist)
                {
                    logger.LogInformation("检测到表不存在，正在应用迁移创建所有表...");
                }
                else
                {
                    logger.LogInformation("发现待应用的迁移: {Migrations}", string.Join(", ", pendingMigrations));
                }

                try
                {
                    await context.Database.MigrateAsync();
                    logger.LogInformation("迁移应用完成，所有表已创建");
                }
                catch (PostgresException pgEx) when (pgEx.SqlState == "42P07")
                {
                    // PostgreSQL 错误 42P07 = duplicate_table (表已存在)
                    logger.LogWarning("检测到表已存在错误 (42P07)，跳过迁移: {Message}", pgEx.Message);
                    logger.LogInformation("如果表结构不匹配，请手动创建迁移来添加缺失字段");
                }
                catch (Exception ex) when (ex.Message.Contains("already exists") ||
                                            ex.Message.Contains("42P07") ||
                                            ex.Message.Contains("已经存在") ||
                                            (ex.InnerException is PostgresException pgInner && pgInner.SqlState == "42P07"))
                {
                    logger.LogWarning("检测到表已存在错误，跳过迁移: {Message}", ex.Message);
                    logger.LogInformation("如果表结构不匹配，请手动创建迁移来添加缺失字段");
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "应用迁移时发生未知错误: {Message}", ex.Message);
                    throw; // 如果迁移失败且表不存在，应该抛出异常
                }
            }
            else if (tablesExist && !migrationHistoryTableExists)
            {
                // 表已存在但迁移历史表不存在，说明是使用 EnsureCreated 创建的
                // 需要手动创建迁移历史表并标记迁移为已应用
                logger.LogWarning("检测到表已存在但迁移历史表不存在，正在创建迁移历史表...");
                try
                {
                    var connection = context.Database.GetDbConnection();
                    if (connection.State != System.Data.ConnectionState.Open)
                    {
                        await connection.OpenAsync();
                    }

                    using var createCommand = connection.CreateCommand();
                    createCommand.CommandText = @"
                        CREATE TABLE IF NOT EXISTS ""__EFMigrationsHistory"" (
                            ""MigrationId"" VARCHAR(150) NOT NULL,
                            ""ProductVersion"" VARCHAR(32) NOT NULL,
                            CONSTRAINT ""PK___EFMigrationsHistory"" PRIMARY KEY (""MigrationId"")
                        );";
                    await createCommand.ExecuteNonQueryAsync();

                    // 获取所有迁移名称并标记为已应用
                    var allMigrations = context.Database.GetMigrations().ToList();
                    foreach (var migration in allMigrations)
                    {
                        createCommand.CommandText = @"
                            INSERT INTO ""__EFMigrationsHistory"" (""MigrationId"", ""ProductVersion"")
                            VALUES ($1, '9.0.10')
                            ON CONFLICT (""MigrationId"") DO NOTHING;";
                        var param = createCommand.CreateParameter();
                        param.ParameterName = "$1";
                        param.Value = migration;
                        createCommand.Parameters.Clear();
                        createCommand.Parameters.Add(param);
                        await createCommand.ExecuteNonQueryAsync();
                    }

                    logger.LogInformation("迁移历史表创建完成，已标记所有迁移为已应用");
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "创建迁移历史表失败: {Message}", ex.Message);
                }
            }
            else
            {
                logger.LogInformation("数据库已是最新版本");
            }

            logger.LogInformation("开始初始化数据库...");

            // 创建科室（三甲医院标准科室设置）- 无论是否有用户数据，都检查并创建科室
            var departments = new List<Department>
            {
                // 临床科室 - 内科系统
                new Department
                {
                    Name = "心血管内科",
                    Code = "XXGNK",
                    Type = "临床科室",
                    Description = "心血管内科",
                    IsActive = true
                },
                new Department
                {
                    Name = "呼吸内科",
                    Code = "HXNK",
                    Type = "临床科室",
                    Description = "呼吸内科",
                    IsActive = true
                },
                new Department
                {
                    Name = "消化内科",
                    Code = "XHNK",
                    Type = "临床科室",
                    Description = "消化内科",
                    IsActive = true
                },
                new Department
                {
                    Name = "神经内科",
                    Code = "SJNK",
                    Type = "临床科室",
                    Description = "神经内科",
                    IsActive = true
                },
                new Department
                {
                    Name = "内分泌科",
                    Code = "NFMK",
                    Type = "临床科室",
                    Description = "内分泌科",
                    IsActive = true
                },
                new Department
                {
                    Name = "肾内科",
                    Code = "SNK",
                    Type = "临床科室",
                    Description = "肾内科",
                    IsActive = true
                },
                new Department
                {
                    Name = "血液内科",
                    Code = "XYNK",
                    Type = "临床科室",
                    Description = "血液内科",
                    IsActive = true
                },
                new Department
                {
                    Name = "风湿免疫科",
                    Code = "FSMYK",
                    Type = "临床科室",
                    Description = "风湿免疫科",
                    IsActive = true
                },
                new Department
                {
                    Name = "感染科",
                    Code = "GRK",
                    Type = "临床科室",
                    Description = "感染科",
                    IsActive = true
                },
                // 临床科室 - 外科系统
                new Department
                {
                    Name = "普通外科",
                    Code = "PTWK",
                    Type = "临床科室",
                    Description = "普通外科",
                    IsActive = true
                },
                new Department
                {
                    Name = "骨科",
                    Code = "GK",
                    Type = "临床科室",
                    Description = "骨科",
                    IsActive = true
                },
                new Department
                {
                    Name = "神经外科",
                    Code = "SJWK",
                    Type = "临床科室",
                    Description = "神经外科",
                    IsActive = true
                },
                new Department
                {
                    Name = "心血管外科",
                    Code = "XXGWK",
                    Type = "临床科室",
                    Description = "心血管外科",
                    IsActive = true
                },
                new Department
                {
                    Name = "胸外科",
                    Code = "XWK",
                    Type = "临床科室",
                    Description = "胸外科",
                    IsActive = true
                },
                new Department
                {
                    Name = "泌尿外科",
                    Code = "MNWK",
                    Type = "临床科室",
                    Description = "泌尿外科",
                    IsActive = true
                },
                new Department
                {
                    Name = "整形外科",
                    Code = "ZXWK",
                    Type = "临床科室",
                    Description = "整形外科",
                    IsActive = true
                },
                // 临床科室 - 其他专科
                new Department
                {
                    Name = "妇产科",
                    Code = "FCK",
                    Type = "临床科室",
                    Description = "妇产科",
                    IsActive = true
                },
                new Department
                {
                    Name = "儿科",
                    Code = "EK",
                    Type = "临床科室",
                    Description = "儿科",
                    IsActive = true
                },
                new Department
                {
                    Name = "新生儿科",
                    Code = "XSEK",
                    Type = "临床科室",
                    Description = "新生儿科",
                    IsActive = true
                },
                new Department
                {
                    Name = "眼科",
                    Code = "YK",
                    Type = "临床科室",
                    Description = "眼科",
                    IsActive = true
                },
                new Department
                {
                    Name = "耳鼻喉科",
                    Code = "EBHK",
                    Type = "临床科室",
                    Description = "耳鼻喉科",
                    IsActive = true
                },
                new Department
                {
                    Name = "口腔科",
                    Code = "KQK",
                    Type = "临床科室",
                    Description = "口腔科",
                    IsActive = true
                },
                new Department
                {
                    Name = "皮肤科",
                    Code = "PFK",
                    Type = "临床科室",
                    Description = "皮肤科",
                    IsActive = true
                },
                new Department
                {
                    Name = "精神科",
                    Code = "JSK",
                    Type = "临床科室",
                    Description = "精神科",
                    IsActive = true
                },
                new Department
                {
                    Name = "康复医学科",
                    Code = "KFYXK",
                    Type = "临床科室",
                    Description = "康复医学科",
                    IsActive = true
                },
                new Department
                {
                    Name = "中医科",
                    Code = "ZYK",
                    Type = "临床科室",
                    Description = "中医科",
                    IsActive = true
                },
                new Department
                {
                    Name = "急诊科",
                    Code = "JZK",
                    Type = "临床科室",
                    Description = "急诊科",
                    IsActive = true
                },
                new Department
                {
                    Name = "ICU",
                    Code = "ICU",
                    Type = "临床科室",
                    Description = "重症医学科（ICU）",
                    IsActive = true
                },
                new Department
                {
                    Name = "CCU",
                    Code = "CCU",
                    Type = "临床科室",
                    Description = "冠心病监护病房",
                    IsActive = true
                },
                new Department
                {
                    Name = "NICU",
                    Code = "NICU",
                    Type = "临床科室",
                    Description = "新生儿重症监护室",
                    IsActive = true
                },
                // 医技科室
                new Department
                {
                    Name = "检验科",
                    Code = "JYK",
                    Type = "医技科室",
                    Description = "检验科",
                    IsActive = true
                },
                new Department
                {
                    Name = "影像科",
                    Code = "YXK",
                    Type = "医技科室",
                    Description = "影像科（放射科）",
                    IsActive = true
                },
                new Department
                {
                    Name = "超声科",
                    Code = "CSK",
                    Type = "医技科室",
                    Description = "超声科",
                    IsActive = true
                },
                new Department
                {
                    Name = "病理科",
                    Code = "BLK",
                    Type = "医技科室",
                    Description = "病理科",
                    IsActive = true
                },
                new Department
                {
                    Name = "药剂科",
                    Code = "YJK",
                    Type = "医技科室",
                    Description = "药剂科",
                    IsActive = true
                },
                new Department
                {
                    Name = "输血科",
                    Code = "SXK",
                    Type = "医技科室",
                    Description = "输血科",
                    IsActive = true
                },
                new Department
                {
                    Name = "核医学科",
                    Code = "HYXK",
                    Type = "医技科室",
                    Description = "核医学科",
                    IsActive = true
                },
                // 行政科室
                new Department
                {
                    Name = "医务科",
                    Code = "YWK",
                    Type = "行政科室",
                    Description = "医务科",
                    IsActive = true
                },
                new Department
                {
                    Name = "护理部",
                    Code = "HLB",
                    Type = "行政科室",
                    Description = "护理部",
                    IsActive = true
                },
                new Department
                {
                    Name = "信息科",
                    Code = "XXK",
                    Type = "行政科室",
                    Description = "信息科",
                    IsActive = true
                },
                new Department
                {
                    Name = "财务科",
                    Code = "CWK",
                    Type = "行政科室",
                    Description = "财务科",
                    IsActive = true
                },
                new Department
                {
                    Name = "人事科",
                    Code = "RSK",
                    Type = "行政科室",
                    Description = "人事科",
                    IsActive = true
                },
                new Department
                {
                    Name = "总务科",
                    Code = "ZWK",
                    Type = "行政科室",
                    Description = "总务科",
                    IsActive = true
                }
            };

            // 检查科室是否已存在
            var existingDepartments = await context.Departments.ToListAsync();
            var newDepartments = departments.Where(d => !existingDepartments.Any(ed => ed.Name == d.Name)).ToList();

            if (newDepartments.Any())
            {
                context.Departments.AddRange(newDepartments);
                await context.SaveChangesAsync();
                logger.LogInformation($"科室创建成功，共创建 {newDepartments.Count} 个科室");
            }
            else
            {
                logger.LogInformation("科室数据已存在，跳过创建");
            }

            // 检查是否已有用户数据
            bool hasUsers = false;
            try
            {
                hasUsers = await context.Users.AnyAsync();
            }
            catch (PostgresException pgEx) when (pgEx.SqlState == "42P01")
            {
                // 42P01 = undefined_table (表不存在)
                logger.LogWarning("Users 表不存在，将创建表并初始化数据");
                hasUsers = false;
            }
            catch (Exception ex) when (ex.Message.Contains("does not exist") || ex.Message.Contains("不存在"))
            {
                logger.LogWarning("表不存在，将创建表并初始化数据: {Message}", ex.Message);
                hasUsers = false;
            }

            if (hasUsers)
            {
                logger.LogInformation("数据库中已存在用户数据，跳过用户和角色初始化");
                return;
            }

            // 创建角色
            var adminRole = new Role
            {
                Name = "Admin",
                Description = "系统管理员",
                Permissions = "[\"*\"]" // 所有权限
            };

            var nurseRole = new Role
            {
                Name = "Nurse",
                Description = "护士",
                Permissions = "[\"patient:read\", \"patient:write\", \"nursing:read\", \"nursing:write\"]"
            };

            var headNurseRole = new Role
            {
                Name = "HeadNurse",
                Description = "护长",
                Permissions = "[\"*:read\", \"*:write\", \"report:read\"]"
            };

            context.Roles.AddRange(adminRole, nurseRole, headNurseRole);
            await context.SaveChangesAsync();

            logger.LogInformation("角色创建成功");

            // 创建管理员用户
            var adminUser = new User
            {
                Username = "admin",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("123456"),
                Name = "系统管理员",
                EmployeeId = "ADMIN001",
                Email = "admin@nissystem.com",
                Department = "信息科",
                Position = "系统管理员",
                RoleId = adminRole.Id,
                IsActive = true
            };

            // 创建测试护士用户
            var nurseUser = new User
            {
                Username = "nurse001",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("123456"),
                Name = "张护士",
                EmployeeId = "NURSE001",
                Email = "nurse001@nissystem.com",
                Department = "内科",
                Position = "护士",
                RoleId = nurseRole.Id,
                IsActive = true
            };

            context.Users.AddRange(adminUser, nurseUser);
            await context.SaveChangesAsync();

            logger.LogInformation("初始用户创建成功");
            logger.LogInformation("管理员账号: admin / 123456");
            logger.LogInformation("护士账号: nurse001 / 123456");

        }
        catch (Exception ex)
        {
            logger.LogError(ex, "数据库初始化失败");
            // 即使前面的初始化失败，也尝试初始化患者数据
            throw;
        }
    }

    /// <summary>
    /// 初始化患者数据（内分泌科20条模拟数据）
    /// </summary>
    private static async Task InitializePatientsAsync(ApplicationDbContext context, ILogger logger)
    {
        logger.LogInformation("开始初始化内分泌科患者数据...");
        
        try
        {
            // 检查表是否存在
            bool tableExists = false;
            try
            {
                await context.Patients.AnyAsync();
                tableExists = true;
            }
            catch (PostgresException pgEx) when (pgEx.SqlState == "42P01")
            {
                logger.LogWarning("Patients 表不存在，跳过患者初始化");
                return;
            }
            catch (Exception ex) when (ex.Message.Contains("does not exist") || ex.Message.Contains("不存在"))
            {
                logger.LogWarning("Patients 表不存在，跳过患者初始化: {Message}", ex.Message);
                return;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "检查 Patients 表时出错: {Message}", ex.Message);
                // 如果表检查失败，尝试继续执行，可能是其他错误
                tableExists = true;
            }

            if (!tableExists)
            {
                logger.LogWarning("Patients 表不存在，跳过患者初始化");
                return;
            }

            // 获取现有的住院号，确保新生成的住院号不冲突
            var existingAdmissionNumbers = new HashSet<string>();
            try
            {
                existingAdmissionNumbers = (await context.Patients
                    .Select(p => p.AdmissionNumber)
                    .ToListAsync())
                    .ToHashSet();
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "获取现有住院号失败，将使用默认编号");
            }

            // 内分泌科常见诊断
            var diagnoses = new[]
            {
                "2型糖尿病",
                "1型糖尿病",
                "糖尿病酮症酸中毒",
                "糖尿病肾病",
                "甲状腺功能亢进症",
                "甲状腺功能减退症",
                "甲状腺结节",
                "桥本甲状腺炎",
                "Graves病",
                "库欣综合征",
                "原发性醛固酮增多症",
                "嗜铬细胞瘤",
                "骨质疏松症",
                "痛风",
                "高尿酸血症",
                "代谢综合征",
                "肥胖症",
                "多囊卵巢综合征",
                "垂体瘤",
                "肾上腺皮质功能减退症"
            };

            // 常见中文姓名（姓氏和名字）
            var surnames = new[] { "王", "李", "张", "刘", "陈", "杨", "赵", "黄", "周", "吴", "徐", "孙", "胡", "朱", "高", "林", "何", "郭", "马", "罗" };
            var givenNames = new[] { "明", "华", "强", "伟", "芳", "敏", "静", "丽", "军", "勇", "艳", "杰", "娟", "涛", "超", "秀", "霞", "平", "刚", "红" };

            var random = new Random();
            var patients = new List<Patient>();
            var admissionNumberBase = 2000; // 住院号起始编号
            var createdCount = 0;

            for (int i = 0; i < 20; i++)
            {
                // 生成唯一的住院号
                string admissionNumber;
                int attempt = 0;
                do
                {
                    admissionNumber = (admissionNumberBase + i + attempt * 100).ToString();
                    attempt++;
                } while (existingAdmissionNumbers.Contains(admissionNumber) && attempt < 100);

                // 如果仍然冲突，使用时间戳
                if (existingAdmissionNumbers.Contains(admissionNumber))
                {
                    admissionNumber = $"NFM{DateTime.UtcNow:yyyyMMdd}{i:D3}{random.Next(100, 999)}";
                }

                existingAdmissionNumbers.Add(admissionNumber);

                var gender = random.Next(2) == 0 ? "男" : "女";
                var age = random.Next(25, 75); // 25-74岁
                var surname = surnames[random.Next(surnames.Length)];
                var givenName = givenNames[random.Next(givenNames.Length)];
                var name = surname + givenName;
                var diagnosis = diagnoses[random.Next(diagnoses.Length)];
                
                // 房间号和床位号
                var roomNumber = $"30{random.Next(1, 6)}"; // 301-305
                var bedNumber = $"{roomNumber}-{random.Next(1, 5)}号"; // 例如：301-1号

                // 护理级别（根据年龄和诊断随机分配）
                var nursingLevels = new[] { "特级", "一级", "二级", "三级" };
                var nursingLevel = age > 65 ? (random.Next(2) == 0 ? "一级" : "二级") : (random.Next(2) == 0 ? "二级" : "三级");

                var admissionDate = DateTime.SpecifyKind(DateTime.UtcNow.AddDays(-random.Next(1, 30)), DateTimeKind.Utc);
                var createdAt = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc);
                
                var patient = new Patient
                {
                    Id = Guid.NewGuid(),
                    Name = name,
                    Gender = gender,
                    Age = age,
                    AdmissionNumber = admissionNumber,
                    Diagnosis = !string.IsNullOrEmpty(diagnosis) ? EncryptionHelper.Encrypt(diagnosis) : null,
                    Department = "内分泌科",
                    RoomNumber = roomNumber,
                    BedNumber = bedNumber,
                    NursingLevel = nursingLevel,
                    Status = "在院",
                    AdmissionDate = admissionDate,
                    CreatedAt = createdAt,
                    CreatedBy = "系统初始化"
                };

                patients.Add(patient);
                createdCount++;
            }

            if (patients.Any())
            {
                try
                {
                    logger.LogInformation($"准备保存 {patients.Count} 条患者记录到数据库...");
                    context.Patients.AddRange(patients);
                    var savedCount = await context.SaveChangesAsync();
                    logger.LogInformation($"✅ 内分泌科患者数据初始化成功！共创建 {createdCount} 条患者记录，保存 {savedCount} 条");
                    
                    // 验证保存结果
                    var savedPatients = await context.Patients
                        .Where(p => p.Department == "内分泌科" && p.CreatedBy == "系统初始化")
                        .CountAsync();
                    logger.LogInformation($"验证：数据库中内分泌科患者数量为 {savedPatients} 条");
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "❌ 保存患者数据时出错: {Message}", ex.Message);
                    logger.LogError("尝试保存的患者数量: {Count}", patients.Count);
                    logger.LogError("异常详情: {StackTrace}", ex.StackTrace);
                    throw;
                }
            }
            else
            {
                logger.LogWarning("⚠️ 未创建新的患者记录，patients 列表为空");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "初始化患者数据失败");
            // 不抛出异常，避免影响其他初始化流程
        }
    }
}

