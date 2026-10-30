using Application.Features.Identity.Abstractions;

using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

using Infrastructure.Persistence.EntityFramework.Contexts;
using Infrastructure.Security.Identity.Domain.Roles;
using Infrastructure.Security.Identity.Domain.Users;
using Infrastructure.Security.Identity.Options;
using Infrastructure.Security.Identity.Services;

namespace Infrastructure.Security.Identity;

/// <summary>Configures ASP.NET Core Identity with the application's security policies.</summary>
public static class IdentityExtensions
{
    /// <summary>
    /// Registers Identity Core, role management, EF Core stores, sign-in services,
    /// token providers, and security-stamp validation.
    /// </summary>
    /// <param name="services">The host's service collection.</param>
    /// <returns>The same collection, for chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="services"/> is <c>null</c>.</exception>
    public static IServiceCollection AddIdentityServices(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddDataProtection()
            .SetApplicationName(IdentitySettingConstant.DataProtection.ApplicationName);

        // Initialize: AddIdentityCore needs the authentication core for SignInManager.
        services.AddAuthentication();

        services.AddIdentityCore<User>(options =>
        {
            options.Password.RequireDigit = IdentitySettingConstant.Password.RequireDigit;
            options.Password.RequireLowercase = IdentitySettingConstant.Password.RequireLowercase;
            options.Password.RequireUppercase = IdentitySettingConstant.Password.RequireUppercase;
            options.Password.RequireNonAlphanumeric = IdentitySettingConstant.Password.RequireNonAlphanumeric;
            options.Password.RequiredLength = IdentitySettingConstant.Password.RequiredLength;
            options.Password.RequiredUniqueChars = IdentitySettingConstant.Password.RequiredUniqueChars;

            options.User.RequireUniqueEmail = IdentitySettingConstant.User.RequireUniqueEmail;
            options.User.AllowedUserNameCharacters = IdentitySettingConstant.User.AllowedUserNameCharacters;

            options.Lockout.DefaultLockoutTimeSpan = IdentitySettingConstant.Lockout.DefaultLockoutTimeSpan;
            options.Lockout.MaxFailedAccessAttempts = IdentitySettingConstant.Lockout.MaxFailedAccessAttempts;
            options.Lockout.AllowedForNewUsers = IdentitySettingConstant.Lockout.AllowedForNewUsers;

            options.SignIn.RequireConfirmedEmail = IdentitySettingConstant.SignIn.RequireConfirmedEmail;
            options.SignIn.RequireConfirmedAccount = IdentitySettingConstant.SignIn.RequireConfirmedAccount;

        })
        .AddRoles<Role>()
        .AddEntityFrameworkStores<ApplicationDbContext>()
        .AddSignInManager()
        .AddDefaultTokenProviders();

        services.Configure<SecurityStampValidatorOptions>(options =>
            options.ValidationInterval = IdentitySettingConstant.SecurityStamp.ValidationInterval);

        services.AddScoped<IRoleManagementService, RoleManagementService>();

        return services;
    }
}
