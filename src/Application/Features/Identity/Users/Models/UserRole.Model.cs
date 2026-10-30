namespace Application.Features.Identity.Users.Models;

#region Request
/// <summary>A request linking one user to one role.</summary>
public abstract record UserRoleModel
{
    public Guid UserId { get; set; }
    public Guid RoleId { get; set; }
}
#endregion