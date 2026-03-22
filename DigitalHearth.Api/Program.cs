using DigitalHearth.Api.BackgroundServices;
using DigitalHearth.Api.Data;
using DigitalHearth.Api.Services;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// EF Core + Npgsql
builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseNpgsql(builder.Configuration.GetConnectionString("Default")));

// Data Protection — persist keys to disk so auth cookies survive restarts
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new System.IO.DirectoryInfo(
        Path.Combine(builder.Environment.ContentRootPath, "keys")));

// CORS — required for session cookies cross-origin
var allowedOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>() ?? [];

builder.Services.AddCors(opt =>
    opt.AddDefaultPolicy(p =>
        p.WithOrigins(allowedOrigins)
         .AllowAnyHeader()
         .AllowAnyMethod()
         .AllowCredentials()));

// Application services
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddScoped<IJoinCodeService, JoinCodeService>();
builder.Services.AddScoped<IPushNotificationService, PushNotificationService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IHouseholdService, HouseholdService>();
builder.Services.AddScoped<IMealService, MealService>();
builder.Services.AddScoped<ITaskService, TaskService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddHostedService<TaskDueCheckerService>();

builder.Services.AddControllers()
    .ConfigureApiBehaviorOptions(opt =>
    {
        // Return { error: "..." } for validation failures instead of ASP.NET ProblemDetails
        opt.InvalidModelStateResponseFactory = ctx =>
        {
            var firstError = ctx.ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .FirstOrDefault() ?? "Invalid request";
            return new Microsoft.AspNetCore.Mvc.BadRequestObjectResult(new { error = firstError });
        };
    });

var app = builder.Build();

// Apply migrations at startup
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
}

app.UseCors();

app.Use(async (context, next) =>
{
    var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
    var method = context.Request.Method;
    var path = context.Request.Path + context.Request.QueryString;

    string body = string.Empty;
    if (context.Request.ContentLength > 0 && context.Request.Body.CanRead)
    {
        context.Request.EnableBuffering();
        using var reader = new System.IO.StreamReader(
            context.Request.Body,
            leaveOpen: true);
        body = await reader.ReadToEndAsync();
        context.Request.Body.Position = 0;
    }

    if (string.IsNullOrEmpty(body))
        logger.LogInformation("→ {Method} {Path}", method, path);
    else
        logger.LogInformation("→ {Method} {Path} | {Body}", method, path, body);

    await next();
    logger.LogInformation("← {StatusCode} {Method} {Path}", context.Response.StatusCode, method, path);
});

app.MapControllers();

app.Run();
