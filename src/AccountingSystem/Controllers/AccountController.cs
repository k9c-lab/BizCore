using System.Security.Claims;
using BizCore.Data;
using BizCore.Models.Entities;
using BizCore.Models.ViewModels;
using BizCore.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BizCore.Controllers;

[Authorize]
public class AccountController : Controller
{
    private readonly AccountingDbContext _context;
    private readonly PasswordHashService _passwordHashService;

    public AccountController(AccountingDbContext context, PasswordHashService passwordHashService)
    {
        _context = context;
        _passwordHashService = passwordHashService;
    }

    [AllowAnonymous]
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
    [AllowAnonymous]
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
            ModelState.AddModelError(string.Empty, "ชื่อผู้ใช้หรือรหัสผ่านไม่ถูกต้อง");
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

    [AllowAnonymous]
    public IActionResult AccessDenied()
    {
        return View();
    }

    public async Task<IActionResult> Profile()
    {
        var user = await GetCurrentUserAsync();
        if (user is null)
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction(nameof(Login));
        }

        return View(BuildProfilePageViewModel(user));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateProfile([Bind(Prefix = "Profile")] AccountProfileUpdateViewModel model)
    {
        var user = await GetCurrentUserAsync();
        if (user is null)
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction(nameof(Login));
        }

        model.DisplayName = model.DisplayName.Trim();
        model.Email = string.IsNullOrWhiteSpace(model.Email) ? null : model.Email.Trim();

        if (!ModelState.IsValid)
        {
            return View("Profile", BuildProfilePageViewModel(user, model, new AccountChangePasswordViewModel()));
        }

        var duplicateEmail = !string.IsNullOrWhiteSpace(model.Email) &&
            await _context.Users.AnyAsync(x => x.Email == model.Email && x.UserId != user.UserId);
        if (duplicateEmail)
        {
            ModelState.AddModelError("Profile.Email", "อีเมลนี้ถูกใช้งานแล้ว");
            return View("Profile", BuildProfilePageViewModel(user, model, new AccountChangePasswordViewModel()));
        }

        user.DisplayName = model.DisplayName;
        user.Email = model.Email;
        await _context.SaveChangesAsync();
        await RefreshSignInAsync(user);

        TempData["ProfileNotice"] = "บันทึกข้อมูลโปรไฟล์เรียบร้อยแล้ว";
        return RedirectToAction(nameof(Profile));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword([Bind(Prefix = "Password")] AccountChangePasswordViewModel model)
    {
        var user = await GetCurrentUserAsync();
        if (user is null)
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction(nameof(Login));
        }

        if (!ModelState.IsValid)
        {
            return View("Profile", BuildProfilePageViewModel(
                user,
                new AccountProfileUpdateViewModel
                {
                    DisplayName = user.DisplayName,
                    Email = user.Email
                },
                model));
        }

        if (!_passwordHashService.VerifyPassword(model.CurrentPassword, user.PasswordHash))
        {
            ModelState.AddModelError("Password.CurrentPassword", "รหัสผ่านปัจจุบันไม่ถูกต้อง");
            return View("Profile", BuildProfilePageViewModel(
                user,
                new AccountProfileUpdateViewModel
                {
                    DisplayName = user.DisplayName,
                    Email = user.Email
                },
                model));
        }

        if (_passwordHashService.VerifyPassword(model.NewPassword, user.PasswordHash))
        {
            ModelState.AddModelError("Password.NewPassword", "รหัสผ่านใหม่ต้องไม่ซ้ำกับรหัสผ่านปัจจุบัน");
            return View("Profile", BuildProfilePageViewModel(
                user,
                new AccountProfileUpdateViewModel
                {
                    DisplayName = user.DisplayName,
                    Email = user.Email
                },
                model));
        }

        user.PasswordHash = _passwordHashService.HashPassword(model.NewPassword);
        await _context.SaveChangesAsync();
        await RefreshSignInAsync(user);

        TempData["ProfileNotice"] = "เปลี่ยนรหัสผ่านเรียบร้อยแล้ว";
        return RedirectToAction(nameof(Profile));
    }

    private IActionResult RedirectToLocal(string? returnUrl)
    {
        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }

        return RedirectToAction("Index", "Welcome");
    }

    private async Task<User?> GetCurrentUserAsync()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdClaim, out var userId))
        {
            return null;
        }

        return await _context.Users
            .Include(x => x.Branch)
            .FirstOrDefaultAsync(x => x.UserId == userId && x.IsActive);
    }

    private async Task RefreshSignInAsync(User user)
    {
        await _context.Entry(user).Reference(x => x.Branch).LoadAsync();

        var principal = BuildPrincipal(user);
        var currentAuth = await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        var authProperties = currentAuth.Properties ?? new AuthenticationProperties();

        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, authProperties);
    }

    private ClaimsPrincipal BuildPrincipal(User user)
    {
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

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        return new ClaimsPrincipal(identity);
    }

    private static AccountProfilePageViewModel BuildProfilePageViewModel(
        User user,
        AccountProfileUpdateViewModel? profile = null,
        AccountChangePasswordViewModel? password = null)
    {
        return new AccountProfilePageViewModel
        {
            Username = user.Username,
            RoleName = user.Role,
            BranchName = user.CanAccessAllBranches && !string.Equals(user.Role, "BranchAdmin", StringComparison.OrdinalIgnoreCase)
                ? "ทุกสาขา"
                : user.Branch?.BranchName ?? "-",
            CanAccessAllBranches = user.CanAccessAllBranches,
            Profile = profile ?? new AccountProfileUpdateViewModel
            {
                DisplayName = user.DisplayName,
                Email = user.Email
            },
            Password = password ?? new AccountChangePasswordViewModel()
        };
    }
}
