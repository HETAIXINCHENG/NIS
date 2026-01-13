using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using NisSystem.API.Data;
using NisSystem.API.Services;

var builder = WebApplication.CreateBuilder(args);

// 添加服务到容器
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // 配置 JSON 序列化选项
        // PropertyNameCaseInsensitive = true 允许接收 camelCase 和 PascalCase
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
        // PropertyNamingPolicy.CamelCase 只影响输出，不影响输入解析
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        // 允许读取数字作为字符串（如果需要）
        options.JsonSerializerOptions.NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowReadingFromString;
        // 处理循环引用：忽略循环引用，避免序列化错误
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
        // 设置最大深度
        options.JsonSerializerOptions.MaxDepth = 64;
    });
builder.Services.AddEndpointsApiExplorer();

// 配置 Swagger/OpenAPI
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "护理信息系统 (NIS) API",
        Version = "v1",
        Description = "基于 ASP.NET Core 9.0 和 EF Core 9.0.10 构建的护理信息系统 RESTful Web API",
        Contact = new OpenApiContact
        {
            Name = "NIS System",
            Email = "support@nissystem.com"
        }
    });

    // 添加 JWT 认证到 Swagger
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token in the text input below.",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// 配置数据库连接
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"),
        npgsqlOptions => npgsqlOptions.MigrationsAssembly("NisSystem.API")));

// 配置 JWT 认证
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = jwtSettings["SecretKey"] ?? "NisSystemSecretKeyForJWTTokenGeneration2024";
var issuer = jwtSettings["Issuer"] ?? "NisSystem";
var audience = jwtSettings["Audience"] ?? "NisSystem";

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = issuer,
        ValidAudience = audience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddAuthorization();

// 注册服务
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<INotificationService, NotificationService>();

// 配置 CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// 配置 HTTP 请求管道
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "NIS System API v1");
        c.RoutePrefix = string.Empty; // 设置 Swagger UI 为根路径
    });
}

app.UseHttpsRedirection();

// 配置静态文件服务（用于访问上传的文件）
// 确保 wwwroot 目录存在
var wwwrootPath = Path.Combine(builder.Environment.ContentRootPath, "wwwroot");
if (!Directory.Exists(wwwrootPath))
{
    Directory.CreateDirectory(wwwrootPath);
}

app.UseStaticFiles();

app.UseCors("AllowAll");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// 确保数据库已创建并初始化数据
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        var logger = services.GetRequiredService<ILogger<Program>>();
        
        // 测试数据库连接
        if (context.Database.CanConnect())
        {
            logger.LogInformation("数据库连接成功");
            
            // 初始化数据库（创建表和初始数据）
            await DbInitializer.InitializeAsync(context, logger);
            
            logger.LogInformation("数据库初始化完成");
        }
        else
        {
            logger.LogWarning("无法连接到数据库，请检查连接字符串");
        }
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "数据库初始化失败: {Message}", ex.Message);
    }
}

app.Run();
