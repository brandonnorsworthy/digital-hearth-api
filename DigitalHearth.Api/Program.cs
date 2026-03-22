using DigitalHearth.Api.BackgroundServices;
using DigitalHearth.Api.Data;
using DigitalHearth.Api.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// EF Core + Npgsql
builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseNpgsql(builder.Configuration.GetConnectionString("Default")));

// Session (in-memory cache backing store — fine for LAN)
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(opt =>
{
    opt.IdleTimeout = TimeSpan.FromMinutes(
        builder.Configuration.GetValue<int>("Session:IdleTimeoutMinutes", 10080));
    opt.Cookie.Name = builder.Configuration["Session:CookieName"] ?? "dh_session";
    opt.Cookie.HttpOnly = true;
    opt.Cookie.SameSite = SameSiteMode.Lax;
    opt.Cookie.IsEssential = true;
    opt.Cookie.SecurePolicy = CookieSecurePolicy.None; // LAN HTTP
});

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
app.UseSession();
app.MapControllers();

app.Run();
