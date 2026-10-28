namespace Application.Common.Security.Authentication.Contexts;

public static class UserContextExtension
{
    public static string? Actor(this IUserContext userContext) => userContext.UserName ?? userContext.UserId?.ToString();
}