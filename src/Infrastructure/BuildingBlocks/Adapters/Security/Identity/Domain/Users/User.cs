using Core.BuildingBlocks.Domain.Events;

using Microsoft.AspNetCore.Identity;

using Core.BuildingBlocks.Modeling.Concerns.Lifecycle.Creatable;
using Core.BuildingBlocks.Modeling.Concerns.Lifecycle.Modifiable;
using Infrastructure.BuildingBlocks.Adapters.Security.Identity.Domain.Users.Claims;
using Infrastructure.BuildingBlocks.Adapters.Security.Identity.Domain.Users.Keys;
using Infrastructure.BuildingBlocks.Adapters.Security.Identity.Domain.Users.Logins;
using Infrastructure.BuildingBlocks.Adapters.Security.Identity.Domain.Users.Roles;
using Infrastructure.BuildingBlocks.Adapters.Security.Identity.Domain.Users.Tokens;

namespace Infrastructure.BuildingBlocks.Adapters.Security.Identity.Domain.Users;

/// <summary>
/// Represents a user account in the system.
/// Inherits from <see cref="IdentityUser{TKey}"/> and implements <see cref="ICreatable"/> and <see cref="IModifiable"/>.
/// </summary>
public class User : IdentityUser<Guid>, ICreatable, IModifiable
{
    private readonly List<IDomainEvent> _events = [];

    /// <summary>Gets the domain events raised by this user.</summary>
    public IReadOnlyCollection<IDomainEvent> Events => _events;

    /// <summary>Clears the recorded domain events after dispatch.</summary>
    public void ClearEvents() => _events.Clear();

    /// <summary>Records a domain event on this user.</summary>
    /// <param name="domainEvent">The event to record.</param>
    internal void RaiseEvent(IDomainEvent domainEvent) => _events.Add(domainEvent);

    /// <summary>Creates a user with the required profile fields and audit metadata.</summary>
    /// <param name="userName">The unique username.</param>
    /// <param name="email">The unique email address.</param>
    /// <param name="firstName">The user's first name.</param>
    /// <param name="lastName">The optional last name.</param>
    /// <param name="phoneNumber">The optional phone number.</param>
    /// <param name="emailConfirmed">Whether the email is already confirmed.</param>
    /// <param name="phoneNumberConfirmed">Whether the phone is already confirmed.</param>
    /// <returns>The created user, or the first broken rule.</returns>
    public static Result<User> Create(
        string userName,
        string email,
        string firstName,
        string? lastName,
        string? phoneNumber = null,
        bool emailConfirmed = false,
        bool phoneNumberConfirmed = false)
    {
        var user = new User
        {
            UserName = userName,
            Email = email,
            FirstName = firstName,
            LastName = lastName,
            PhoneNumber = phoneNumber,
            EmailConfirmed = emailConfirmed,
            PhoneNumberConfirmed = phoneNumberConfirmed,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            CreatedBy = "System"
        };

        return UserValidator
            .Validate(user)
            .Tap(created => created.RaiseEvent(
                new UserCreated(created.Id, created.UserName!, created.Email!)));
    }

    #region Properties

    /// <summary>
    /// Gets or sets the user's first name.
    /// </summary>
    public string FirstName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the user's last name.
    /// </summary>
    public string? LastName { get; set; }

    /// <summary>
    /// Gets or sets the user's date of birth.
    /// </summary>
    public DateTimeOffset? DateOfBirth { get; set; }

    /// <summary>
    /// Gets the user's full name (FirstName + LastName).
    /// </summary>
    public string FullName =>
        string.IsNullOrWhiteSpace(LastName)
            ? FirstName
            : $"{FirstName} {LastName}";

    #endregion

    #region Account Status

    /// <summary>
    /// Gets or sets a value indicating whether the user account is active.
    /// </summary>
    public bool IsActive { get; set; } = UserConstant.Defaults.IsActive;

    /// <summary>
    /// Gets or sets the UTC timestamp of the user's last login.
    /// </summary>
    public DateTimeOffset? LastLoginAtUtc { get; set; }

    #endregion

    #region Sign-In Tracking

    /// <summary>
    /// Gets or sets the IP address from the current sign-in session.
    /// </summary>
    public string? CurrentSignInIp { get; set; }

    /// <summary>
    /// Gets or sets the UTC timestamp of the current sign-in.
    /// </summary>
    public DateTimeOffset? CurrentSignInAtUtc { get; set; }

    /// <summary>
    /// Gets or sets the IP address from the previous sign-in session.
    /// </summary>
    public string? LastSignInIp { get; set; }

    /// <summary>
    /// Gets or sets the UTC timestamp of the previous sign-in.
    /// </summary>
    public DateTimeOffset? LastSignInAtUtc { get; set; }

    /// <summary>
    /// Gets or sets the cumulative sign-in count for the user.
    /// </summary>
    public int SignInCount { get; set; }

    #endregion

    #region Auditing
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset? ModifiedAtUtc { get; set; }
    public string? CreatedBy { get; set; }
    public string? ModifiedBy { get; set; }
    #endregion Auditing

    #region Navigation

    /// <summary>
    /// Gets or sets the collection of custom claims assigned to the user.
    /// </summary>
    public ICollection<UserClaim> Claims { get; set; } = [];

    /// <summary>
    /// Gets or sets the collection of roles assigned to the user.
    /// </summary>
    public ICollection<UserRole> UserRoles { get; set; } = [];

    /// <summary>
    /// Gets or sets the collection of external logins for the user.
    /// </summary>
    public ICollection<UserLogin> UserLogins { get; set; } = [];

    /// <summary>
    /// Gets or sets the collection of security tokens for the user.
    /// </summary>
    public ICollection<UserToken> UserTokens { get; set; } = [];

    /// <summary>
    /// Gets or sets the collection of passkeys for the user.
    /// </summary>
    public ICollection<UserPasskey> Passkeys { get; set; } = [];

    #endregion
}