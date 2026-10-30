// using Application.Features.Identity.Users.Models;

// namespace Application.Features.Identity.Users.Mappings;

// public static class UserMapper
// {
//     #region Spec
//     public static CreateUserRequestModel ToSpec(CreateUserModel request)
//     {
//         ArgumentNullException.ThrowIfNull(request);

//         return new CreateUserRequestModel
//         {
//             UserName = request.UserName.Trim(),
//             Email = request.Email.Trim(),
//             Password = request.Password,
//             FirstName = Normalize(request.FirstName),
//             LastName = Normalize(request.LastName),
//             EmailConfirmed = request.EmailConfirmed,
//             RoleIds = request.RoleIds,
//         };
//     }

//     public static UpdateUserRequestModel ToSpec(UpdateUserModel request)
//     {
//         ArgumentNullException.ThrowIfNull(request);

//         return new UpdateUserRequestModel {
//             UserName = request.UserName.Trim(),
//             Email = request.Email.Trim(),
//             FirstName = Normalize(request.FirstName),
//             LastName = Normalize(request.LastName),
//             RoleIds = request.RoleIds,
//         };
//     }

//     private static string? Normalize(string? value) =>
//         string.IsNullOrWhiteSpace(value) ? null : value.Trim();
// }
