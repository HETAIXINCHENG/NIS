# 护理信息系统 (NIS) API

基于 ASP.NET Core 9.0 和 EF Core 9.0.10 构建的 RESTful Web API，使用 PostgreSQL 18 作为数据库。

## 功能特性

- ✅ RESTful API 设计
- ✅ Swagger/OpenAPI 文档
- ✅ JWT 身份认证和授权
- ✅ 数据加密存储（敏感信息）
- ✅ 基于角色的权限控制
- ✅ EF Core 9.0.10 数据访问
- ✅ PostgreSQL 18 数据库支持

## 技术栈

- **框架**: ASP.NET Core 9.0
- **ORM**: Entity Framework Core 9.0.10
- **数据库**: PostgreSQL 18
- **认证**: JWT Bearer Token
- **加密**: AES 加密 + BCrypt 密码哈希
- **API 文档**: Swagger/OpenAPI

## 项目结构

```
NisSystem.API/
├── Controllers/          # API 控制器
│   ├── AuthController.cs
│   ├── PatientsController.cs
│   └── VitalSignsController.cs
├── Data/                 # 数据访问层
│   └── ApplicationDbContext.cs
├── DTOs/                 # 数据传输对象
│   ├── LoginDto.cs
│   ├── RegisterDto.cs
│   └── AuthResponseDto.cs
├── Helpers/              # 辅助类
│   └── EncryptionHelper.cs
├── Models/               # 实体模型
│   ├── BaseEntity.cs
│   ├── User.cs
│   ├── Role.cs
│   ├── Patient.cs
│   ├── VitalSigns.cs
│   ├── NursingAssessment.cs
│   ├── NursingRecord.cs
│   ├── MedicationOrder.cs
│   ├── MedicationExecution.cs
│   ├── NursingPlan.cs
│   ├── ShiftHandover.cs
│   ├── Schedule.cs
│   └── AuditLog.cs
├── Services/             # 业务服务
│   ├── IJwtService.cs
│   ├── JwtService.cs
│   ├── IAuthService.cs
│   └── AuthService.cs
├── Program.cs            # 应用程序入口
└── appsettings.json      # 配置文件
```

## 快速开始

### 前置要求

- .NET 9.0 SDK
- PostgreSQL 18
- Visual Studio 2022 或 VS Code

### 安装步骤

1. **克隆或下载项目**

2. **配置数据库连接**

   编辑 `appsettings.json` 文件，修改数据库连接字符串：

   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Host=localhost;Port=5432;Database=NisSystem;Username=postgres;Password=your_password"
     }
   }
   ```

3. **创建数据库**

   在 PostgreSQL 中创建数据库：

   ```sql
   CREATE DATABASE "NisSystem";
   ```

4. **运行应用程序**

   ```bash
   cd NisSystem.API
   dotnet run
   ```

5. **访问 Swagger UI**

   打开浏览器访问：`https://localhost:5001` 或 `http://localhost:5000`

## API 端点

### 认证端点

- `POST /api/auth/login` - 用户登录
- `POST /api/auth/register` - 用户注册

### 患者管理端点

- `GET /api/patients` - 获取患者列表
- `GET /api/patients/{id}` - 获取患者详情
- `POST /api/patients` - 创建患者（需要 Nurse 或 HeadNurse 角色）
- `PUT /api/patients/{id}` - 更新患者信息（需要 Nurse 或 HeadNurse 角色）
- `DELETE /api/patients/{id}` - 删除患者（需要 HeadNurse 角色）

### 生命体征端点

- `GET /api/vitalsigns/patient/{patientId}` - 获取患者生命体征记录
- `POST /api/vitalsigns` - 创建生命体征记录（需要 Nurse 或 HeadNurse 角色）

## 使用示例

### 1. 用户注册

```bash
POST /api/auth/register
Content-Type: application/json

{
  "username": "nurse001",
  "password": "password123",
  "name": "张护士",
  "employeeId": "N001",
  "email": "nurse001@hospital.com",
  "department": "内科",
  "position": "护士",
  "roleName": "Nurse"
}
```

### 2. 用户登录

```bash
POST /api/auth/login
Content-Type: application/json

{
  "username": "nurse001",
  "password": "password123"
}
```

响应：

```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "userId": "guid",
  "username": "nurse001",
  "name": "张护士",
  "role": "Nurse",
  "employeeId": "N001",
  "department": "内科",
  "position": "护士"
}
```

### 3. 使用 Token 访问受保护的 API

```bash
GET /api/patients
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

## 数据模型

### 核心实体

- **User** - 用户（护士、护师、护长等）
- **Role** - 角色
- **Patient** - 患者
- **VitalSigns** - 生命体征记录
- **NursingAssessment** - 护理评估
- **NursingRecord** - 护理措施记录
- **MedicationOrder** - 医嘱
- **MedicationExecution** - 用药执行记录
- **NursingPlan** - 护理计划
- **ShiftHandover** - 交接班记录
- **Schedule** - 排班记录
- **AuditLog** - 审计日志

## 安全特性

### 数据加密

敏感信息（如患者诊断、过敏史、用药史、联系信息）使用 AES 加密存储。

### 密码哈希

用户密码使用 BCrypt 进行哈希存储。

### JWT 认证

- Token 有效期：24 小时（可在配置中修改）
- 支持基于角色的授权（Role-Based Authorization）

### 软删除

所有实体支持软删除，通过 `IsDeleted` 字段标记。

## 角色和权限

- **Nurse** - 护士：可以创建和更新患者信息、记录生命体征等
- **HeadNurse** - 护长：拥有所有护士权限，还可以删除患者
- **Doctor** - 医生：查看权限

## 配置说明

### JWT 设置

在 `appsettings.json` 中配置：

```json
{
  "JwtSettings": {
    "SecretKey": "your-secret-key-here",
    "Issuer": "NisSystem",
    "Audience": "NisSystem",
    "ExpiryMinutes": "1440"
  }
}
```

### 数据库迁移

使用 EF Core Migrations 管理数据库架构：

```bash
# 创建迁移
dotnet ef migrations add InitialCreate --project NisSystem.API

# 应用迁移
dotnet ef database update --project NisSystem.API
```

## 开发说明

### 添加新的 API 端点

1. 在 `Controllers` 文件夹中创建新的控制器
2. 使用 `[Authorize]` 特性保护端点
3. 使用 `[Authorize(Roles = "RoleName")]` 限制特定角色访问

### 添加新的实体

1. 在 `Models` 文件夹中创建实体类，继承 `BaseEntity`
2. 在 `ApplicationDbContext` 中添加 `DbSet`
3. 在 `OnModelCreating` 中配置关系

## 注意事项

1. **生产环境配置**：
   - 修改 JWT SecretKey 为强密钥
   - 修改 AES 加密密钥和 IV
   - 使用环境变量存储敏感配置
   - 启用 HTTPS

2. **数据库**：
   - 定期备份数据库
   - 配置连接池
   - 优化查询性能

3. **安全**：
   - 定期更新依赖包
   - 实施 API 速率限制
   - 记录审计日志

## 许可证

本项目仅供学习和研究使用。

## 联系方式

如有问题或建议，请联系开发团队。

