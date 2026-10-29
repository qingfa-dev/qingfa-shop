using Infrastructure.Persistence.EntityFramework.Contexts;
using Infrastructure.Security.Identity;
using Infrastructure.Security.Identity.Domain.Roles;
using Infrastructure.Security.Identity.Domain.Users;
using Infrastructure.Security.Identity.Options;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Infrastructure.Unit.Tests.Security.Identity;

[Trait("Category", "Unit")]
[Trait("Module", "Identity")]
[Trait("Feature", "Composition")]
public sealed class IdentityExtensionTest
{
    [Fact(DisplayName = "AddIdentityServices should register the Identity managers and stores")]
    public void AddIdentityServices_ShouldRegisterManagersAndStores()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseInMemoryDatabase($"Identity_{Guid.NewGuid():N}"));

        services.AddIdentityServices();

        using ServiceProvider provider = services.BuildServiceProvider();
        provider.GetRequiredService<UserManager<User>>().Should().NotBeNull();
        provider.GetRequiredService<RoleManager<Role>>().Should().NotBeNull();
        provider.GetRequiredService<SignInManager<User>>().Should().NotBeNull();
        provider.GetRequiredService<IUserStore<User>>().Should().NotBeNull();
        provider.GetRequiredService<IRoleStore<Role>>().Should().NotBeNull();
    }

    [Fact(DisplayName = "AddIdentityServices should apply the configured password and lockout policy")]
    public void AddIdentityServices_ShouldApplyPolicy()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseInMemoryDatabase($"Identity_{Guid.NewGuid():N}"));
        services.AddIdentityServices();
        using ServiceProvider provider = services.BuildServiceProvider();

        IdentityOptions options = provider.GetRequiredService<IOptions<IdentityOptions>>().Value;

        options.Password.RequiredLength.Should().Be(IdentitySettingConstant.Password.RequiredLength);
        options.Password.RequireNonAlphanumeric
            .Should().Be(IdentitySettingConstant.Password.RequireNonAlphanumeric);
        options.Lockout.MaxFailedAccessAttempts
            .Should().Be(IdentitySettingConstant.Lockout.MaxFailedAccessAttempts);
        options.User.RequireUniqueEmail.Should().BeTrue();
    }

    [Fact(DisplayName = "AddIdentityServices should reject null services")]
    public void AddIdentityServices_ShouldRejectNull()
    {
        IServiceCollection? services = null;

        Assert.Throws<ArgumentNullException>(() => services!.AddIdentityServices());
    }
}
