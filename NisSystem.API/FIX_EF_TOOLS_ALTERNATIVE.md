# 修复 EF Core 工具更新错误

## 问题
更新 `dotnet-ef` 工具时出现错误：`Settings file 'DotnetToolSettings.xml' was not found in the package`

## 解决方案

### 方法 1：卸载后重新安装（推荐）

```bash
# 1. 卸载现有工具
dotnet tool uninstall --global dotnet-ef

# 2. 重新安装
dotnet tool install --global dotnet-ef --version 9.0.10
```

### 方法 2：清除 NuGet 缓存后重试

```bash
# 清除 NuGet 缓存
dotnet nuget locals all --clear

# 重新安装工具
dotnet tool install --global dotnet-ef --version 9.0.10
```

### 方法 3：使用项目级别的工具（如果全局工具仍有问题）

在 `NisSystem.API.csproj` 中添加工具引用：

```xml
<ItemGroup>
  <PackageReference Include="Microsoft.EntityFrameworkCore.Tools" Version="9.0.10">
    <PrivateAssets>all</PrivateAssets>
    <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
  </PackageReference>
</ItemGroup>
```

然后使用：
```bash
dotnet ef --version
```

## 重要提示

**迁移已经成功应用！** 工具版本警告不会影响应用程序运行。如果应用程序已经正常工作，可以暂时忽略这个警告。

只有在需要创建新迁移时才需要更新工具。

