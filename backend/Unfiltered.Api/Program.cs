using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Unfiltered.Api.Data;
using Unfiltered.Api.Dtos;
using Unfiltered.Api.Models;
using Unfiltered.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(o => o.UseSqlServer(builder.Configuration.GetConnectionString("Default")));
builder.Services.AddControllers()
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddSingleton<IPasswordHasher<User>, PasswordHasher<User>>();
builder.Services.AddSingleton<FileStore>();
builder.Services.AddHostedService<ProofCleanupService>();
builder.Services.Configure<FormOptions>(o => o.MultipartBodyLengthLimit = 12 * 1024 * 1024);

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(o =>
    {
        o.Cookie.Name = "unfiltered.auth";
        o.Cookie.HttpOnly = true;
        // Strict SameSite keeps the cookie off cross-site requests, which protects the API from CSRF.
        o.Cookie.SameSite = SameSiteMode.Strict;
        o.Cookie.SecurePolicy = builder.Environment.IsDevelopment() ? CookieSecurePolicy.SameAsRequest : CookieSecurePolicy.Always;
        o.ExpireTimeSpan = TimeSpan.FromDays(7);
        o.SlidingExpiration = true;
        // An API answers with status codes instead of redirecting to a login page.
        o.Events.OnRedirectToLogin = ctx => { ctx.Response.StatusCode = StatusCodes.Status401Unauthorized; return Task.CompletedTask; };
        o.Events.OnRedirectToAccessDenied = ctx => { ctx.Response.StatusCode = StatusCodes.Status403Forbidden; return Task.CompletedTask; };
    });
builder.Services.AddAuthorization();

builder.Services.AddRateLimiter(o =>
{
    o.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    o.AddPolicy("auth", ctx => RateLimitPartition.GetFixedWindowLimiter(
        ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 10, Window = TimeSpan.FromMinutes(1) }));
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();

    // dotnet run -- make-mod someone@example.com
    if (args is ["make-mod", var email, ..])
    {
        var updated = await db.Users
            .Where(u => u.Email == email.Trim().ToLowerInvariant())
            .ExecuteUpdateAsync(s => s.SetProperty(u => u.Role, UserRole.Moderator));
        Console.WriteLine(updated > 0 ? $"{email} is now a moderator. They need to log out and back in." : $"No account found for {email}.");
        return;
    }

    if (app.Configuration.GetValue<bool>("Seed:DemoData"))
        await DemoSeeder.SeedAsync(db, scope.ServiceProvider.GetRequiredService<IPasswordHasher<User>>());
}

app.UseExceptionHandler(e => e.Run(async ctx =>
{
    ctx.Response.StatusCode = StatusCodes.Status500InternalServerError;
    await ctx.Response.WriteAsJsonAsync(new ErrorResponse("Something went wrong on our side. Please try again."));
}));

app.Use(async (ctx, next) =>
{
    ctx.Response.Headers.XContentTypeOptions = "nosniff";
    ctx.Response.Headers.XFrameOptions = "DENY";
    ctx.Response.Headers["Referrer-Policy"] = "same-origin";
    await next();
});

// The React build is copied into wwwroot for production; in development Vite serves it.
app.UseDefaultFiles();
app.UseStaticFiles();

app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.Map("/api/{**rest}", () => Results.NotFound(new ErrorResponse("Not found.")));
app.MapFallbackToFile("index.html");

app.Run();
