using System.Security.Claims;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Unfiltered.Api.Data;
using Unfiltered.Api.Dtos;
using Unfiltered.Api.Infrastructure;
using Unfiltered.Api.Models;

namespace Unfiltered.Api.Controllers;

[Route("api/auth")]
public partial class AuthController(AppDbContext db, IPasswordHasher<User> hasher) : ApiControllerBase
{
    [GeneratedRegex(@"^[^\s@]+@[^\s@]+\.[^\s@]+$")]
    private static partial Regex EmailFormat();

    [HttpGet("me")]
    public async Task<MeResponse> Me()
    {
        var id = User.UserId();
        var user = id is null ? null : await db.Users.FindAsync(id);
        return new MeResponse(user?.ToDto());
    }

    [HttpPost("register")]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> Register(RegisterRequest req)
    {
        var email = (req.Email ?? "").Trim().ToLowerInvariant();
        var password = req.Password ?? "";
        var displayName = (req.DisplayName ?? "").Trim();

        if (email.Length > 256 || !EmailFormat().IsMatch(email)) return Fail("Enter a valid email address.");
        if (password.Length < 8) return Fail("Use a password of at least 8 characters.");
        if (displayName.Length is < 2 or > 40) return Fail("Your public name must be 2 to 40 characters. It can be a nickname.");
        if (await db.Users.AnyAsync(u => u.Email == email)) return Fail("An account with this email already exists. Log in instead.", StatusCodes.Status409Conflict);

        var user = new User { Email = email, DisplayName = displayName };
        user.PasswordHash = hasher.HashPassword(user, password);
        db.Users.Add(user);
        await db.SaveChangesAsync();

        await SignInAsync(user);
        return Ok(new MeResponse(user.ToDto()));
    }

    [HttpPost("login")]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> Login(LoginRequest req)
    {
        var email = (req.Email ?? "").Trim().ToLowerInvariant();
        var user = await db.Users.SingleOrDefaultAsync(u => u.Email == email);
        if (user is null || hasher.VerifyHashedPassword(user, user.PasswordHash, req.Password ?? "") == PasswordVerificationResult.Failed)
            return Fail("Wrong email or password.", StatusCodes.Status401Unauthorized);

        await SignInAsync(user);
        return Ok(new MeResponse(user.ToDto()));
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return NoContent();
    }

    private Task SignInAsync(User user)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.DisplayName),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role.ToString()),
        };
        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        return HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));
    }
}
