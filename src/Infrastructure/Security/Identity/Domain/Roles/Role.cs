using Core.Common.Domain.Events;

using Microsoft.AspNetCore.Identity;

using Core.Common.Concerns.Lifecycle.Creatable;
using Core.Common.Concerns.Lifecycle.Modifiable;
using Infrastructure.Security.Identity.Domain.Roles.Claims;
using Infrastructure.Security.Identity.Domain.Users.Roles;

namespace Infrastructure.Security.Identity.Domain.Roles;

public class Role : IdentityRole<Guid>, ICreatable, IModifiable
{
    private readonly List<IDomainEvent> _events = [];

    /// <summary>Gets the domain events raised by this role.</summary>
    public IReadOnlyCollection<IDomainEvent> Events => _events;

    /// <summary>Clears the recorded domain events after dispatch.</summary>
    public void ClearEvents() => _events.Clear();

    /// <summary>Records a domain event on this role.</summary>
    /// <param name="domainEvent">The event to record.</param>
    internal void RaiseEvent(IDomainEvent domainEvent) => _events.Add(domainEvent);

    /// <summary>Creates a role with audit metadata.</summary>
    /// <param name="name">The unique role name.</param>
    /// <param name="description">The optional role description.</param>
    /// <returns>The created role, or the first broken rule.</returns>
    public static Result<Role> Create(string name, string? description = null)
    {
        var role = new Role
        {
            Name = name,
            Description = description,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            CreatedBy = "System"
        };

        return RoleValidator
            .Validate(role)
            .Tap(created => created.RaiseEvent(new RoleCreated(created.Id, created.Name!)));
    }

    #region Properties
    /// <summary>
    /// Gets or sets the description of the role.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this is a system-protected role.
    /// System roles cannot be modified or deleted.
    /// </summary>
    public bool IsSystem { get; set; }

    #endregion Properties


    #region Auditing
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset? ModifiedAtUtc { get; set; }
    public string? CreatedBy { get; set; }
    public string? ModifiedBy { get; set; }
    #endregion Auditing

    #region Navigation

    // Navigation

    /// <summary>
    /// Gets or sets the collection of user-role mappings for this role.
    /// </summary>
    public ICollection<UserRole> UserRoles { get; set; } = [];

    /// <summary>
    /// Gets or sets the collection of role claims (permissions) for this role.
    /// </summary>
    public ICollection<RoleClaim> RoleClaims { get; set; } = [];

    #endregion Navigation
}