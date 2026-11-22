using AutoMapper;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using System.Security.Claims;
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
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using TruthEye.Security;
var builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
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

      var jwtKey = builder.Configuration["Jwt:Key"];
if (string.IsNullOrWhiteSpace(jwtKey) || jwtKey.Length < 32)
    throw new InvalidOperationException("Jwt:Key is missing or shorter than 32 characters. Set it with user-secrets or an environment variable.");

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
        },

        // فحص حالة الحساب مع كل طلب (كاش 30 ثانية)
       OnTokenValidated = async context =>
{
    if (!int.TryParse(context.Principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var uid))
    {
        context.Fail("Invalid token");
        return;
    }

    var services = context.HttpContext.RequestServices;
    var cache = services.GetRequiredService<IMemoryCache>();

    var state = await cache.GetOrCreateAsync($"state:{uid}", async entry =>
    {
        entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(30);
        var db = services.GetRequiredService<AppDbContext>();
        return await db.Users.AsNoTracking()
            .Where(u => u.Id == uid && !u.IsDeleted)
            .Select(u => new UserState(u.IsActive, u.MustChangePassword))
            .FirstOrDefaultAsync();
    });

    if (state is not { Active: true })
    {
        context.Fail("Account disabled");
        return;
    }

    var sid = context.Principal!.FindFirst("te_sid")?.Value ?? "";
    if (sid.Length > 0)
    {
        var db = services.GetRequiredService<AppDbContext>();
        var alive = await cache.GetOrCreateAsync($"sid:{sid}", async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(20);
            var now = DateTime.UtcNow;
            var s = await db.UserSessions.FirstOrDefaultAsync(x => x.Sid == sid && x.EndedAt == null);
            if (s == null) return false;
            s.LastSeenAt = now;                // النبض: مرة كل 20 ثانية على الأكتر
            await db.SaveChangesAsync();
            return true;
        });

        if (alive != true)
        {
            context.Fail("Signed in from another device");
            return;
        }
    }

    if (state.MustChange)
        ((ClaimsIdentity)context.Principal!.Identity!).AddClaim(new Claim("must_change_password", "1"));
}
    };
});
builder.Services.AddAuthorization();
builder.Services.AddRateLimiter(o =>
{
    o.RejectionStatusCode = 429;
    o.AddPolicy("auth", ctx => RateLimitPartition.GetFixedWindowLimiter(
        ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 10,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0
        }));
    o.OnRejected = async (c, ct) =>
    {
        c.HttpContext.Response.ContentType = "application/json";
        await c.HttpContext.Response.WriteAsync(
            "{\"success\":false,\"error\":\"Too many requests. Try again in a minute.\",\"errorCode\":\"RATE_LIMITED\"}", ct);
    };
});

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        b => b.MigrationsAssembly("TruthEye"))
);

builder.Services.AddDistributedMemoryCache();
builder.Services.AddSignalR();

builder.Services.AddAutoMapper(cfg => cfg.AddMaps(typeof(MappingProfile).Assembly));
builder.Services.AddMemoryCache();
builder.Services.AddHttpContextAccessor();
// Register Services
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IEmailSender, SmtpEmailSender>();
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
builder.Services.AddHostedService<LogRetentionWorker>();
builder.Services.AddScoped<ICourseEnrollmentService, CourseEnrollmentService>();
builder.Services.AddScoped<IChatLockService, ChatLockService>();
builder.Services.AddScoped<ISessionReviewService, SessionReviewService>();
builder.Services.AddScoped<IGradingService, GradingService>();
builder.Services.AddScoped<IAppealService, AppealService>();
builder.Services.AddScoped<IAnalyticsService, AnalyticsService>();
builder.Services.AddScoped<IResumeService, ResumeService>();
// CORS: ONE AddCors call. The origins below are always allowed, plus anything listed in Cors:Origins.
var configuredOrigins = builder.Configuration.GetSection("Cors:Origins").Get<string[]>() ?? Array.Empty<string>();

var corsOrigins = configuredOrigins
    .Concat(new[] { "http://localhost:3000","http://localhost:3001", "https://truth-eye-r4na-seven.vercel.app" })
    .Select(o => o.Trim().TrimEnd('/'))
    .Where(o => o.Length > 0)
    .Distinct(StringComparer.OrdinalIgnoreCase)
    .ToArray();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.WithOrigins(corsOrigins)
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

// وضع الاسترجاع للطوارئ: dotnet run -- recover-superadmin <email>
// يتشغّل على السيرفر بس، ومايفتحش الـ web server. الرابط بيتطبع في الكونسول مرة واحدة
if (args.Length == 2 && args[0] == "recover-superadmin")
{
    using var scope = app.Services.CreateScope();
    var auth = scope.ServiceProvider.GetRequiredService<TruthEye.Services.IAuthService>();
    var link = await auth.RecoverSuperAdminAsync(args[1]);
    Console.WriteLine("Two-factor cleared and sessions ended for the university president.");
    Console.WriteLine("One-time password reset link (valid for a limited time):");
    Console.WriteLine(link);
    return;
}

// Must be the FIRST middleware: it restores the real client IP and scheme (http/https)
// when the API runs behind a reverse proxy (nginx, IIS, a load balancer...)
var forwarded = new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto,
    ForwardLimit = 1
};
// Only proxies listed in Proxy:KnownProxies may set client headers; anyone else is the direct client
foreach (var proxy in builder.Configuration.GetSection("Proxy:KnownProxies").Get<string[]>() ?? Array.Empty<string>())
{
    if (System.Net.IPAddress.TryParse(proxy, out var proxyAddress))
        forwarded.KnownProxies.Add(proxyAddress);
}
app.UseForwardedHeaders(forwarded);

app.UseExceptionHandlingMiddleware();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// CORS must run before the HTTPS redirect: a browser rejects any redirect on a preflight request
app.UseCors("AllowAll");

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseSerilogRequestLogging();
app.UseRateLimiter();

// Authentication & Authorization middleware - ORDER IS IMPORTANT!
app.UseAuthentication();
app.UseMiddleware<PasswordChangeMiddleware>();
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