# 数据库设置说明

## PostgreSQL 数据库初始化

### 1. 安装 PostgreSQL 18

确保已安装 PostgreSQL 18 数据库服务器。

### 2. 创建数据库

使用 PostgreSQL 客户端（如 pgAdmin 或 psql）执行以下命令：

```sql
-- 创建数据库
CREATE DATABASE "NisSystem";

-- 如果需要创建开发环境数据库
CREATE DATABASE "NisSystem";
```

### 3. 配置连接字符串

编辑 `appsettings.json` 文件，修改连接字符串：

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=NisSystem;Username=postgres;Password=your_password"
  }
}
```

### 4. 运行应用程序

应用程序启动时会自动创建数据库表结构（使用 `EnsureCreated()` 方法）。

### 5. 使用 EF Core Migrations（推荐）

对于生产环境，建议使用 EF Core Migrations 管理数据库架构：

#### 安装 EF Core 工具（如果尚未安装）

```bash
dotnet tool install --global dotnet-ef
```

#### 创建初始迁移

```bash
cd NisSystem.API
dotnet ef migrations add InitialCreate
```

#### 应用迁移

```bash
dotnet ef database update
```

#### 查看迁移历史

```bash
dotnet ef migrations list
```

#### 回滚迁移

```bash
dotnet ef database update PreviousMigrationName
```

### 6. 初始化种子数据（可选）

可以创建一个数据库种子类来初始化默认角色和用户：

```csharp
// 在 Program.cs 中添加种子数据初始化
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var context = services.GetRequiredService<ApplicationDbContext>();
    
    // 确保数据库已创建
    context.Database.EnsureCreated();
    
    // 添加种子数据
    if (!context.Roles.Any())
    {
        context.Roles.AddRange(
            new Role { Name = "Nurse", Description = "护士", Permissions = "[]" },
            new Role { Name = "HeadNurse", Description = "护长", Permissions = "[]" },
            new Role { Name = "Doctor", Description = "医生", Permissions = "[]" }
        );
        context.SaveChanges();
    }
}
```

## 数据库备份和恢复

### 备份数据库

```bash
pg_dump -U postgres -d NisSystem -F c -f backup.dump
```

### 恢复数据库

```bash
pg_restore -U postgres -d NisSystem -c backup.dump
```

## 性能优化建议

1. **索引优化**：为常用查询字段添加索引
2. **连接池**：配置适当的连接池大小
3. **查询优化**：使用 `Include()` 预加载关联数据，避免 N+1 查询问题
4. **软删除**：使用查询过滤器自动过滤已删除的记录

## 安全建议

1. **连接字符串**：使用环境变量或密钥管理服务存储敏感信息
2. **最小权限原则**：数据库用户只授予必要的权限
3. **定期备份**：设置自动备份策略
4. **审计日志**：启用 PostgreSQL 审计日志功能

