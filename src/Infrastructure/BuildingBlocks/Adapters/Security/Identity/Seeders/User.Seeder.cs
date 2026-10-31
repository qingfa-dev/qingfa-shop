using Application.BuildingBlocks.Persistence.Initialization;
using Application.BuildingBlocks.Persistence.Initialization.Seeders;

using Infrastructure.BuildingBlocks.Adapters.Persistence.EntityFramework.Contexts;
using Infrastructure.BuildingBlocks.Adapters.Security.Identity.Domain.Roles;
using Infrastructure.BuildingBlocks.Adapters.Security.Identity.Domain.Users;
using Infrastructure.BuildingBlocks.Adapters.Security.Identity.Domain.Users.Roles;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.BuildingBlocks.Adapters.Security.Identity.Seeders;

/// <summary>Seeds the default administrator, manager, and user accounts.</summary>
public sealed class UserSeeder(ApplicationDbContext dbContext) : IPersistenceSeeder<User, Guid>
{
    /// <inheritdoc />
    public string Name => IdentitySeederConstant.Names.Users;

    /// <inheritdoc />
    public string DisplayName => "Seed Identity Users";

    /// <inheritdoc />
    public int Order => IdentitySeederConstant.Order.Users;

    /// <inheritdoc />
    public string[] DependsOn => [IdentitySeederConstant.Names.Roles];

    /// <inheritdoc />
    public bool IsRequired => false;

    Type? IPersistenceSeeder.EntityType => typeof(User);

    Type? IPersistenceSeeder.KeyType => typeof(Guid);


    /// <inheritdoc />
    public async Task<Result> ExecuteAsync(
        IInitializationContext context,
        CancellationToken cancellationToken = default)
    {
        if (await dbContext.Set<User>().AnyAsync(cancellationToken).ConfigureAwait(false))
            return Result.Ok();

        Role? adminRole = await dbContext.Set<Role>()
            .FirstOrDefaultAsync(role => role.Name == RoleConstant.Defaults.Admin, cancellationToken)
            .ConfigureAwait(false);
        Role? managerRole = await dbContext.Set<Role>()
            .FirstOrDefaultAsync(role => role.Name == RoleConstant.Defaults.Manager, cancellationToken)
            .ConfigureAwait(false);
        Role? userRole = await dbContext.Set<Role>()
            .FirstOrDefaultAsync(role => role.Name == RoleConstant.Defaults.User, cancellationToken)
            .ConfigureAwait(false);

        if (adminRole is null || managerRole is null || userRole is null)
            return Result.Fail(RoleOutcome.Failure.NotFound);

        DateTimeOffset now = DateTimeOffset.UtcNow;
        var hasher = new PasswordHasher<User>();
        User admin = CreateUser("admin", "admin@qingfa.shop", "Admin", "User", hasher, "Admin@123!", now);
        User manager = CreateUser("manager", "manager@qingfa.shop", "Manager", "User", hasher, "Manager@123!", now);
        User user = CreateUser("user", "user@qingfa.shop", "Standard", "User", hasher, "User@123!", now);

        dbContext.Set<User>().AddRange(admin, manager, user);
        dbContext.Set<UserRole>().AddRange(
            new UserRole { UserId = admin.Id, RoleId = adminRole.Id },
            new UserRole { UserId = manager.Id, RoleId = managerRole.Id },
            new UserRole { UserId = user.Id, RoleId = userRole.Id });

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Result.Ok();
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<InitializationOperation>>> PreviewAsync(
        IInitializationContext context,
        CancellationToken cancellationToken = default)
    {
        bool hasUsers = await dbContext.Set<User>().AnyAsync(cancellationToken).ConfigureAwait(false);
        if (hasUsers)
        {
            return Result<IReadOnlyList<InitializationOperation>>.Ok(
            [
                new InitializationOperation
                {
                    StepName = Name,
                    Description = "Identity users already exist. No changes needed.",
                    Type = InitializationOperationType.DataCheck
                }
            ]);
        }

        return Result<IReadOnlyList<InitializationOperation>>.Ok(
        [
            new InitializationOperation
            {
                StepName = Name,
                Description = "Insert default admin, manager, and user accounts.",
                Type = InitializationOperationType.DataChange,
                EstimatedRowsAffected = 3
            }
        ]);
    }

    private static User CreateUser(
        string userName,
        string email,
        string firstName,
        string lastName,
        PasswordHasher<User> hasher,
        string password,
        DateTimeOffset now)
    {
        Result<User> result = User.Create(userName, email, firstName, lastName);
        if (result.IsFailure)
        {
            throw new InvalidOperationException(
                $"Failed to create user '{userName}': {result.Errors[0].Code} - {result.Errors[0].Message}");
        }

        User user = result.Value;
        user.NormalizedUserName = userName.ToUpperInvariant();
        user.NormalizedEmail = email.ToUpperInvariant();
        user.EmailConfirmed = true;
        user.IsActive = true;
        user.SecurityStamp = Guid.NewGuid().ToString();
        user.ConcurrencyStamp = Guid.NewGuid().ToString();
        user.CreatedAtUtc = now;
        user.CreatedBy = "System";
        user.PasswordHash = hasher.HashPassword(user, password);
        return user;
    }
}
