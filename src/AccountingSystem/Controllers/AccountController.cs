using System.Security.Claims;
using BizCore.Data;
using BizCore.Models.ViewModels;
using BizCore.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BizCore.Controllers;

[AllowAnonymous]
public class AccountController : Controller
{
    private readonly AccountingDbContext _context;
    private readonly PasswordHashService _passwordHashService;

    public AccountController(AccountingDbContext context, PasswordHashService passwordHashService)
    {
        _context = context;
        _passwordHashService = passwordHashService;
    }

    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToLocal(returnUrl);
        }

        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var username = model.Username.Trim();
        var user = await _context.Users
            .AsNoTracking()
            .Include(x => x.Branch)
            .FirstOrDefaultAsync(x => x.Username == username && x.IsActive);

        if (user is null || !_passwordHashService.VerifyPassword(model.Password, user.PasswordHash))
        {
            ModelState.AddModelError(string.Empty, "Invalid username or password.");
            return View(model);
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.UserId.ToString()),
            new(ClaimTypes.Name, user.Username),
            new(ClaimTypes.GivenName, user.DisplayName),
            new(ClaimTypes.Role, user.Role),
            new("BranchId", user.BranchId?.ToString() ?? string.Empty),
            new("BranchCode", user.Branch?.BranchCode ?? string.Empty),
            new("BranchName", user.Branch?.BranchName ?? string.Empty),
            new("CanAccessAllBranches", user.CanAccessAllBranches ? "true" : "false")
        };

        try
        {
            var permissions = await _context.RolePermissions
                .AsNoTracking()
                .Include(x => x.Permission)
                .Where(x => x.RoleName == user.Role && x.Permission != null)
                .Select(x => x.Permission!.Code)
                .ToListAsync();

            claims.AddRange(permissions.Select(x => new Claim("Permission", x)));
        }
        catch (Exception ex) when (ex is DbUpdateException || ex is Microsoft.Data.SqlClient.SqlException || ex is InvalidOperationException)
        {
            // Permission tables are additive. Existing role-based login still works before the DB script is applied.
        }

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);
        var authProperties = new AuthenticationProperties
        {
            IsPersistent = model.RememberMe,
            ExpiresUtc = model.RememberMe ? DateTimeOffset.UtcNow.AddDays(7) : null
        };

        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, authProperties);
        return RedirectToLocal(model.ReturnUrl);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction(nameof(Login));
    }

    public IActionResult AccessDenied()
    {
        return View();
    }

    private IActionResult RedirectToLocal(string? returnUrl)
    {
        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }

        return RedirectToAction("Index", "Home");
    }
}
