using System.Reflection;
using System.Runtime.CompilerServices;

using Application.Common.Querying;
using Application.Common.Querying.Pagination;
using Application.Features.Identity.Abstractions;
using Application.Features.Identity.Auth.Models;
using Application.Features.Identity.External.Abstractions;
using Application.Features.Identity.External.Models;
using Application.Features.Identity.Permissions.Models;
using Application.Features.Identity.Roles.Models;
using Application.Features.Identity.Users.Models;

namespace Application.Unit.Tests.Features.Identity.Ports;

public sealed class IdentityPortsTest
{
    public static TheoryData<Type, string[]> ModelShapes => new()
    {
        {
            typeof(UserModel),
            [
                "Id", "UserName", "Email", "FirstName", "LastName", "EmailConfirmed",
                "TwoFactorEnabled", "IsActive", "IsLockedOut", "LockoutEndUtc", "Roles",
            ]
        },
        {
            typeof(UserSummaryModel),
            ["Id", "UserName", "Email", "IsActive", "IsLockedOut", "Roles"]
        },
        {
            typeof(RoleModel),
            ["Id", "Name", "Permissions"]
        },
        {
            typeof(PermissionItemModel),
            ["Identifier", "Domain", "Category", "Resource", "Action", "Description"]
        },
        {
            typeof(PermissionSnapshotModel),
            ["Permissions", "RefreshedOnUtc"]
        },
        {
            typeof(ExternalChallengeModel),
            ["Provider", "RedirectUri"]
        },
        {
            typeof(ExternalCallbackModel),
            ["RedirectUrl"]
        },
    };

    public static TheoryData<Type> SpecTypes => new()
    {
        typeof(CreateUserSpec),
        typeof(UpdateUserSpec),
        typeof(CreateRoleSpec),
        typeof(UpdateRoleSpec),
    };

    [Theory(DisplayName = "Model Exposes Expected Fields")]
    [MemberData(nameof(ModelShapes))]
    public void Model_ShouldExposeExpectedFields(Type model, string[] expected)
    {
        model.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Select(property => property.Name)
            .Should().Equal(expected);
    }

    [Theory(DisplayName = "Spec Is Sealed Init Only Record")]
    [MemberData(nameof(SpecTypes))]
    public void Spec_ShouldBeImmutable(Type spec)
    {
        spec.IsSealed.Should().BeTrue();
        spec.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Should().NotBeEmpty();

        foreach (PropertyInfo property in spec.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            MethodInfo? setter = property.SetMethod;
            bool isInitOnly = setter is null
                || setter.ReturnParameter
                    .GetRequiredCustomModifiers()
                    .Contains(typeof(IsExternalInit));

            isInitOnly.Should().BeTrue($"{spec.Name}.{property.Name} must be init-only");
        }
    }

    [Fact(DisplayName = "External Code Store Exposes Store And Consume Only")]
    public void ExternalCodeStore_ShouldExposeStoreAndConsumeOnly()
    {
        MethodInfo[] methods = typeof(IExternalCodeStore)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance);

        methods.Should().HaveCount(2);
        methods.Select(method => method.Name)
            .Should().BeEquivalentTo(["StoreAsync", "ConsumeAsync"]);

        MethodInfo store = methods.Single(method => method.Name == "StoreAsync");
        store.ReturnType.Should().Be(typeof(Task<Result<string>>));
        store.GetParameters().Select(parameter => parameter.ParameterType)
            .Should().Equal([typeof(AuthTokenResponse), typeof(CancellationToken)]);

        MethodInfo consume = methods.Single(method => method.Name == "ConsumeAsync");
        consume.ReturnType.Should().Be(typeof(Task<Result<AuthTokenResponse>>));
        consume.GetParameters().Select(parameter => parameter.ParameterType)
            .Should().Equal([typeof(string), typeof(CancellationToken)]);
    }

    [Fact(DisplayName = "User Account Service Exposes Admin User Operations")]
    public void UserAccountService_ShouldExposeAdminUserOperations()
        => AssertPort(
            typeof(IUserAccountService),
            ("SearchAsync", typeof(Task<Result<QueryPage<UserModel>>>)),
            ("GetAsync", typeof(Task<Result<UserModel>>)),
            ("CreateAsync", typeof(Task<Result<UserModel>>)),
            ("UpdateAsync", typeof(Task<Result<UserModel>>)),
            ("DeleteAsync", typeof(Task<Result>)),
            ("SetActiveAsync", typeof(Task<Result<UserModel>>)),
            ("AssignRoleAsync", typeof(Task<Result<UserModel>>)),
            ("RemoveRoleAsync", typeof(Task<Result<UserModel>>)),
            ("ResetPasswordAsync", typeof(Task<Result>)),
            ("ConfirmEmailAsync", typeof(Task<Result>)),
            ("SetLockedAsync", typeof(Task<Result<UserModel>>)),
            ("RevokeSessionsAsync", typeof(Task<Result>)));

    [Fact(DisplayName = "Role Management Service Exposes Role Operations")]
    public void RoleManagementService_ShouldExposeRoleOperations()
        => AssertPort(
            typeof(IRoleManagementService),
            ("SearchAsync", typeof(Task<Result<QueryPage<RoleModel>>>)),
            ("GetAsync", typeof(Task<Result<RoleModel>>)),
            ("CreateAsync", typeof(Task<Result<RoleModel>>)),
            ("UpdateAsync", typeof(Task<Result<RoleModel>>)),
            ("DeleteAsync", typeof(Task<Result>)),
            ("AssignPermissionAsync", typeof(Task<Result<RoleModel>>)),
            ("RemovePermissionAsync", typeof(Task<Result<RoleModel>>)));

    [Fact(DisplayName = "Permission Catalog Service Exposes Catalog Operations")]
    public void PermissionCatalogService_ShouldExposeCatalogOperations()
        => AssertPort(
            typeof(IPermissionCatalogService),
            ("SearchAsync", typeof(Task<Result<PermissionSnapshotModel>>)),
            ("GetAsync", typeof(Task<Result<PermissionItemModel>>)),
            ("ListDomainsAsync", typeof(Task<Result<IReadOnlyList<string>>>)),
            ("ListCategoriesAsync", typeof(Task<Result<IReadOnlyList<string>>>)),
            ("GetUserPermissionsAsync", typeof(Task<Result<IReadOnlyList<string>>>)),
            ("GetRolePermissionsAsync", typeof(Task<Result<IReadOnlyList<string>>>)),
            ("CheckUserPermissionsAsync", typeof(Task<Result<bool>>)),
            ("RefreshAsync", typeof(Task<Result<PermissionSnapshotModel>>)));

    private static void AssertPort(
        Type port,
        params (string Name, Type ReturnType)[] expected)
    {
        MethodInfo[] methods = port.GetMethods(BindingFlags.Public | BindingFlags.Instance);

        methods.Should().HaveCount(expected.Length);
        methods.Select(method => method.Name)
            .Should().BeEquivalentTo(expected.Select(method => method.Name));

        Dictionary<string, Type> returnTypes = methods
            .ToDictionary(method => method.Name, method => method.ReturnType);

        foreach ((string name, Type returnType) in expected)
        {
            returnTypes.Should().ContainKey(name);
            returnTypes[name].Should().Be(returnType, $"{port.Name}.{name} return type");
        }
    }
}
