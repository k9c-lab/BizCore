using BizCore.Data;
using BizCore.Services;
using System.Globalization;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add(new AuthorizeFilter());
});
builder.Services.AddDbContext<AccountingDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("AccountingDb")));
builder.Services.Configure<CompanyProfileSettings>(builder.Configuration.GetSection("CompanyProfile"));
builder.Services.Configure<SiteBrandingSettings>(builder.Configuration.GetSection("SiteBranding"));
builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("Email"));
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<PasswordHashService>();
builder.Services.AddScoped<IUserPermissionService, UserPermissionService>();
builder.Services.AddScoped<ISystemSettingService, SystemSettingService>();
builder.Services.AddScoped<IDatabaseMigrationService, DatabaseMigrationService>();
builder.Services.AddScoped<IEmailSender, SmtpEmailSender>();
builder.Services.AddScoped<PurchaseWorkflowEmailService>();
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "BizCore.Auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.SlidingExpiration = true;
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.Events = new CookieAuthenticationEvents
        {
            OnSigningIn = context =>
            {
                var logger = context.HttpContext.RequestServices
                    .GetRequiredService<ILoggerFactory>()
                    .CreateLogger("AuthDebug");

                logger.LogInformation(
                    "Cookie signing in for user {UserName}. Persistent={IsPersistent}, ExpiresUtc={ExpiresUtc}, Path={Path}",
                    context.Principal?.Identity?.Name ?? "(unknown)",
                    context.Properties?.IsPersistent ?? false,
                    context.Properties?.ExpiresUtc,
                    context.HttpContext.Request.Path.Value);

                return Task.CompletedTask;
            },
            OnSignedIn = context =>
            {
                var logger = context.HttpContext.RequestServices
                    .GetRequiredService<ILoggerFactory>()
                    .CreateLogger("AuthDebug");

                logger.LogInformation(
                    "Cookie signed in for user {UserName}. Response path={Path}",
                    context.Principal?.Identity?.Name ?? "(unknown)",
                    context.HttpContext.Request.Path.Value);

                return Task.CompletedTask;
            },
            OnValidatePrincipal = context =>
            {
                var logger = context.HttpContext.RequestServices
                    .GetRequiredService<ILoggerFactory>()
                    .CreateLogger("AuthDebug");

                logger.LogDebug(
                    "Validating auth cookie for user {UserName}. Request={Url}",
                    context.Principal?.Identity?.Name ?? "(unknown)",
                    context.HttpContext.Request.GetDisplayUrl());

                return Task.CompletedTask;
            },
            OnRedirectToLogin = context =>
            {
                var logger = context.HttpContext.RequestServices
                    .GetRequiredService<ILoggerFactory>()
                    .CreateLogger("AuthDebug");

                logger.LogWarning(
                    "Redirecting to login. Request={Url}, RedirectUri={RedirectUri}, Authenticated={Authenticated}, CookieNames={CookieNames}",
                    context.Request.GetDisplayUrl(),
                    context.RedirectUri,
                    context.HttpContext.User.Identity?.IsAuthenticated ?? false,
                    string.Join(", ", context.Request.Cookies.Keys.OrderBy(x => x)));

                context.Response.Redirect(context.RedirectUri);
                return Task.CompletedTask;
            },
            OnRedirectToAccessDenied = context =>
            {
                var logger = context.HttpContext.RequestServices
                    .GetRequiredService<ILoggerFactory>()
                    .CreateLogger("AuthDebug");

                logger.LogWarning(
                    "Redirecting to access denied. Request={Url}, RedirectUri={RedirectUri}, User={UserName}",
                    context.Request.GetDisplayUrl(),
                    context.RedirectUri,
                    context.HttpContext.User.Identity?.Name ?? "(unknown)");

                context.Response.Redirect(context.RedirectUri);
                return Task.CompletedTask;
            }
        };
    });
builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    var culture = new CultureInfo("en-US");
    options.DefaultRequestCulture = new RequestCulture(culture);
    options.SupportedCultures = new[] { culture };
    options.SupportedUICultures = new[] { culture };
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRequestLocalization();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();
app.Use(async (context, next) =>
{
    await next();

    if (context.Response.StatusCode is 400 or 401 or 403)
    {
        var logger = context.RequestServices
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger("AuthDebug");

        logger.LogWarning(
            "Request completed with status {StatusCode}. Method={Method}, Url={Url}, Authenticated={Authenticated}, User={UserName}, CookieNames={CookieNames}",
            context.Response.StatusCode,
            context.Request.Method,
            context.Request.GetDisplayUrl(),
            context.User.Identity?.IsAuthenticated ?? false,
            context.User.Identity?.Name ?? "(anonymous)",
            string.Join(", ", context.Request.Cookies.Keys.OrderBy(x => x)));
    }
});

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Welcome}/{action=Index}/{id?}");

app.Run();
