# 初始用户设置说明

## 问题

数据库中的 `Users` 表没有数据，导致无法使用 `admin / 123456` 登录。

## 解决方案

已创建数据库初始化器，会在应用启动时自动创建初始用户和角色。

## 操作步骤

### 1. 重启后端 API

停止当前运行的后端（如果正在运行），然后重新启动：

```bash
cd NisSystem/NisSystem.API
dotnet run
```

### 2. 查看初始化日志

启动后端后，查看控制台输出，应该看到：

```
info: 数据库连接成功
info: 开始初始化数据库...
info: 角色创建成功
info: 初始用户创建成功
info: 管理员账号: admin / 123456
info: 护士账号: nurse001 / 123456
info: 数据库初始化完成
```

### 3. 使用初始账号登录

初始化完成后，可以使用以下账号登录：

#### 管理员账号
- **用户名**: `admin`
- **密码**: `123456`
- **角色**: Admin（系统管理员）

#### 护士账号
- **用户名**: `nurse001`
- **密码**: `123456`
- **角色**: Nurse（护士）

## 初始数据说明

初始化器会自动创建：

### 角色（Roles）
1. **Admin** - 系统管理员（所有权限）
2. **Nurse** - 护士（患者和护理相关权限）
3. **HeadNurse** - 护长（读取和写入权限，报表权限）

### 用户（Users）
1. **admin** - 系统管理员
   - 工号: ADMIN001
   - 科室: 信息科
   - 职位: 系统管理员

2. **nurse001** - 测试护士
   - 工号: NURSE001
   - 科室: 内科
   - 职位: 护士

## 注意事项

1. **仅首次运行**：如果数据库中已有用户数据，初始化器会跳过创建，避免重复。

2. **密码安全**：初始密码是 `123456`，建议在生产环境中修改。

3. **数据库连接**：确保 PostgreSQL 数据库正在运行，并且连接字符串正确。

## 验证

登录成功后，应该能够：
- ✅ 访问工作台（Dashboard）
- ✅ 查看患者管理页面
- ✅ 使用其他功能模块

## 如果初始化失败

如果看到错误信息，请检查：

1. **数据库连接**：确保 PostgreSQL 正在运行
2. **连接字符串**：检查 `appsettings.json` 中的连接字符串
3. **数据库权限**：确保数据库用户有创建表和插入数据的权限
4. **查看详细错误**：查看控制台的完整错误堆栈

## 手动创建用户（备选方案）

如果自动初始化失败，可以使用注册接口创建用户：

```bash
# 使用 curl 或 Postman
POST http://localhost:5137/api/auth/register
Content-Type: application/json

{
  "username": "admin",
  "password": "123456",
  "name": "系统管理员",
  "employeeId": "ADMIN001",
  "roleName": "Admin",
  "department": "信息科",
  "position": "系统管理员"
}
```
# 创建迁移
dotnet ef migrations add InitialCreate

# 应用迁移
dotnet ef database update


Add-Migration AddMissingNursingAssessmentFields
Update-Database
