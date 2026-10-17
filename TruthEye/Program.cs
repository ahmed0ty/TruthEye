using AutoMapper;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using System.Text;
using System.Text.Json.Serialization;
using TruthEye.API.Hubs;
using TruthEye.Data;
using TruthEye.Entities;
using TruthEye.Entities.Data;
using TruthEye.Mapping;
using TruthEye.Middleware;
using TruthEye.Services;
using TruthEye.UnitOfWorkk;

var builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/log-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

// Add HttpClient for AI Evaluation Service
builder.Services.AddHttpClient<AIEvaluationService>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(30);
});

builder.Services.AddControllers()
      .AddJsonOptions(options =>
      {
          options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
          options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
      });

// Configure Authentication
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
options.TokenValidationParameters = new TokenValidationParameters
{
    ValidateIssuer = true,
    ValidateAudience = true,
    ValidateLifetime = true,
    ValidateIssuerSigningKey = true,
    ValidIssuer = builder.Configuration["Jwt:Issuer"],
    ValidAudience = builder.Configuration["Jwt:Audience"],
    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"])),
    ClockSkew = TimeSpan.FromSeconds(30)
};

    // السماح لـ SignalR بقراءة الـ token من الـ query string
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var accessToken = context.Request.Query["access_token"];
            var path = context.HttpContext.Request.Path;

            if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
            {
                context.Token = accessToken;
            }

            return Task.CompletedTask;
        }
    };
});
builder.Services.AddAuthorization();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        b => b.MigrationsAssembly("TruthEye"))
);

builder.Services.AddDistributedMemoryCache();
builder.Services.AddSignalR();

builder.Services.AddAutoMapper(typeof(MappingProfile));
builder.Services.AddMemoryCache();

// Register Services
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IExamService, ExamService>();
builder.Services.AddScoped<IExamSessionService, ExamSessionService>();
builder.Services.AddScoped<IAILogService, AILogService>();
builder.Services.AddScoped<IReportService, ReportService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<ISupportTicketService, SupportTicketService>();
builder.Services.AddScoped<ISystemLogService, SystemLogService>();
builder.Services.AddSingleton<ICacheService, CacheService>();
builder.Services.AddScoped<ICourseService, CourseService>();
builder.Services.AddScoped<IAIEvaluationService, AIEvaluationService>();
builder.Services.AddHostedService<SessionExpiryWorker>();
builder.Services.AddSingleton<IEvidenceStore, LocalEvidenceStore>();
builder.Services.AddSingleton<ISettingsService, SettingsService>();
builder.Services.AddHostedService<EvidenceRetentionWorker>();
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.WithOrigins(builder.Configuration.GetSection("Cors:Origins").Get<string[]>()
              ?? new[] { "http://localhost:3000" })
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials()
              .WithExposedHeaders("Content-Disposition");
    });
});

builder.Services.AddHealthChecks()
    .AddDbContextCheck<AppDbContext>("database");

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Online Exam Proctoring API",
        Version = "v1",
        Description = "API for Online Exam Proctoring System with AI-based monitoring, Real-time Chat, Notifications, and Caching"
    });

    // Add JWT Authentication to Swagger
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token",
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

var app = builder.Build();

// Must be the FIRST middleware: it restores the real client IP and scheme (http/https)
// when the API runs behind a reverse proxy (nginx, IIS, a load balancer...)
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor
                     | ForwardedHeaders.XForwardedProto
});

// Seed Super Admin
//using (var scope = app.Services.CreateScope())
//{
//    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

//    if (!await context.SuperAdmins.AnyAsync())
//    {
//        var role = await context.Roles.FirstOrDefaultAsync(r => r.Name == "SuperAdmin");
//        if (role == null)
//        {
//            role = new Role { Name = "SuperAdmin" };
//            context.Roles.Add(role);
//            await context.SaveChangesAsync();
//        }

//        var admin = new SuperAdmin
//        {
//            FirstName = "Super",
//            LastName = "Admin",
//            Email = "admin@examproctoring.com",
//            NationalId = "00000000000000",
//            PhoneNumber = "+201000000000",
//            CreatedAt = DateTime.UtcNow,
//            IsActive = true,
//            TotalActionsPerformed = 0,
//            ProfileImage = "default-profile.png",
//            RoleId = role.Id
//        };

//        var passwordHasher = new PasswordHasher<SuperAdmin>();
//        admin.PasswordHash = passwordHasher.HashPassword(admin, "Admin@123456");

//        context.SuperAdmins.Add(admin);
//        await context.SaveChangesAsync();
//    }
//}

app.UseExceptionHandlingMiddleware();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseSerilogRequestLogging();
app.UseCors("AllowAll");

// Authentication & Authorization middleware - ORDER IS IMPORTANT!
app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/health");
app.MapHub<ChatHub>("/hubs/chat");
app.MapHub<NotificationHub>("/hubs/notification");
app.MapHub<ExamMonitoringHub>("/hubs/exam-monitoring");
app.MapControllers();

try
{
    Log.Information("Starting Online Exam Proctoring API with AI, SignalR, and Caching");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}