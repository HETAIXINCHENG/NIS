# 迁移设置指南

## 问题
EF Core 找不到迁移文件，导致无法创建数据库表。

## 解决方案

### 方法 1：使用 dotnet CLI 创建迁移（推荐）

1. **打开终端/命令提示符**，进入项目目录：
   ```bash
   cd NisSystem.API
   ```

2. **确保已安装 EF Core 工具**：
   ```bash
   dotnet tool install --global dotnet-ef
   dotnet tool install --global dotnet-ef --version 9.0.10

   # 1. 卸载现有工具
dotnet tool uninstall --global dotnet-ef

# 2. 重新安装
dotnet tool install --global dotnet-ef --version 9.0.10

3. **创建初始迁移**：
   ```bash
   dotnet ef migrations add InitialCreate
   ```

4. **应用迁移到数据库**：
   ```bash
   dotnet ef database update
   ```

### 方法 2：使用 Visual Studio Package Manager Console

1. 在 Visual Studio 中，打开 **工具** → **NuGet 包管理器** → **程序包管理器控制台**

2. 确保 **默认项目** 选择为 `NisSystem.API`

3. 运行以下命令：
   ```powershell
   Add-Migration InitialCreate
   Update-Database
   ```

### 方法 3：如果迁移已存在但未编译

如果迁移文件已存在但未被识别，请：

1. **清理并重新生成项目**：
   ```bash
   dotnet clean
   dotnet build
   ```

2. **重新应用迁移**：
   ```bash
   dotnet ef database update
   ```

## 验证

迁移创建成功后，应该会在 `Migrations` 文件夹中看到：
- `[时间戳]_InitialCreate.cs` - 迁移文件
- `[时间戳]_InitialCreate.Designer.cs` - 设计器文件
- `ApplicationDbContextModelSnapshot.cs` - 模型快照

## 如果仍然失败

如果上述方法都不行，可以尝试：

1. **删除 Migrations 文件夹**（如果存在）
2. **重新创建迁移**：
   ```bash
   dotnet ef migrations add InitialCreate --force
   ```
3. **应用迁移**：
   ```bash
   dotnet ef database update
   ```

