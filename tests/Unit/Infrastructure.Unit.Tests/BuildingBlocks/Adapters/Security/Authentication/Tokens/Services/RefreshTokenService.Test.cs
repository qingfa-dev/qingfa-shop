using Application.BuildingBlocks.Security.Authentication.Tokens;
using Application.BuildingBlocks.Security.Authentication.Tokens.Abstractions.Refresh;
using Application.BuildingBlocks.Security.Authentication.Tokens.Domain;
using Application.BuildingBlocks.Security.Authentication.Tokens.Options;

using Infrastructure.BuildingBlocks.Adapters.Security.Authentication.Tokens.Services;

namespace Infrastructure.Unit.Tests.BuildingBlocks.Adapters.Security.Authentication.Tokens.Services;

[Trait("Category", "Unit")]
[Trait("Module", "Security")]
[Trait("Feature", "Authentication")]
public sealed class RefreshTokenServiceTest
{
    private static readonly DateTimeOffset Now = new(2020, 1, 1, 0, 0, 0, TimeSpan.Zero);
    private static readonly Error StoreError = Error.Unexpected("Test.StoreFailure", "Store failed.");

    private static RefreshTokenService CreateService(FakeRefreshTokenStore store) =>
        new(store, new RefreshTokenOption { LifetimeDays = 7 });

    private static void ShouldFailWith(Result result, string expectedCode)
    {
        result.IsFailure.Should().BeTrue();
        result.Errors[0].Code.Should().Be(expectedCode);
    }

    [Fact(DisplayName = "IssueAsync should create and persist an active token")]
    public async Task IssueAsync_ShouldCreateAndPersistToken()
    {
        var store = new FakeRefreshTokenStore();
        RefreshTokenService service = CreateService(store);

        Result<RefreshTokenIssue> result = await service.IssueAsync(" user-1 ", Now);

        result.IsSuccess.Should().BeTrue();
        RefreshTokenIssue issue = result.Value;
        issue.Token.Should().StartWith(TokenConstant.Defaults.RefreshTokenPrefix);
        issue.Entity.TokenHash.Should().HaveLength(64);
        issue.Entity.TokenHash.Should().NotBe(issue.Token);
        issue.Entity.UserId.Should().Be("user-1");
        issue.Entity.CreatedAtUtc.Should().Be(Now);
        issue.Entity.ExpiresAtUtc.Should().Be(Now.AddDays(7));
        issue.Entity.IsActive(Now).Should().BeTrue();
        store.Tokens.Should().ContainSingle().Which.Should().BeSameAs(issue.Entity);
        store.AddCount.Should().Be(1);
    }

    [Fact(DisplayName = "IssueAsync should reuse the supplied token family")]
    public async Task IssueAsync_ShouldUseSuppliedFamily()
    {
        var store = new FakeRefreshTokenStore();
        RefreshTokenService service = CreateService(store);
        Guid familyId = Guid.NewGuid();

        Result<RefreshTokenIssue> result = await service.IssueAsync("user-1", Now, familyId);

        result.IsSuccess.Should().BeTrue();
        result.Value.TokenFamilyId.Should().Be(familyId);
    }

    [Theory(DisplayName = "IssueAsync should reject a missing user identifier")]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task IssueAsync_ShouldRejectMissingUser(string? userId)
    {
        var store = new FakeRefreshTokenStore();
        RefreshTokenService service = CreateService(store);

        Result<RefreshTokenIssue> result = await service.IssueAsync(userId!, Now);

        ShouldFailWith(result, "Token.UserId.Required");
        store.Tokens.Should().BeEmpty();
    }

    [Fact(DisplayName = "IssueAsync should propagate a store failure")]
    public async Task IssueAsync_ShouldPropagateStoreFailure()
    {
        var store = new FakeRefreshTokenStore { FailAdd = true };
        RefreshTokenService service = CreateService(store);

        Result<RefreshTokenIssue> result = await service.IssueAsync("user-1", Now);

        ShouldFailWith(result, "Test.StoreFailure");
    }

    [Fact(DisplayName = "FindAsync should return the token matching the plaintext value")]
    public async Task FindAsync_ShouldReturnStoredToken()
    {
        var store = new FakeRefreshTokenStore();
        RefreshTokenService service = CreateService(store);
        RefreshTokenIssue issue = (await service.IssueAsync("user-1", Now)).Value;

        Result<RefreshToken> result = await service.FindAsync(issue.Token);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeSameAs(issue.Entity);
    }

    [Fact(DisplayName = "FindAsync should return not found for an unknown token")]
    public async Task FindAsync_ShouldReturnNotFound()
    {
        var store = new FakeRefreshTokenStore();
        RefreshTokenService service = CreateService(store);

        Result<RefreshToken> result = await service.FindAsync("rt_unknown");

        ShouldFailWith(result, "RefreshToken.NotFound");
    }

    [Theory(DisplayName = "FindAsync should reject a missing token")]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task FindAsync_ShouldRejectMissingToken(string? token)
    {
        var store = new FakeRefreshTokenStore();
        RefreshTokenService service = CreateService(store);

        Result<RefreshToken> result = await service.FindAsync(token!);

        ShouldFailWith(result, "Token.RefreshToken.Required");
    }

    [Fact(DisplayName = "FindAsync should reject an overlong token")]
    public async Task FindAsync_ShouldRejectOverlongToken()
    {
        var store = new FakeRefreshTokenStore();
        RefreshTokenService service = CreateService(store);
        string token = new('a', TokenConstant.Limits.MaxRefreshTokenLength + 1);

        Result<RefreshToken> result = await service.FindAsync(token);

        ShouldFailWith(result, "Token.RefreshToken.TooLong");
    }

    [Fact(DisplayName = "FindAsync should propagate a store failure")]
    public async Task FindAsync_ShouldPropagateStoreFailure()
    {
        var store = new FakeRefreshTokenStore { FailFind = true };
        RefreshTokenService service = CreateService(store);

        Result<RefreshToken> result = await service.FindAsync("rt_unknown");

        ShouldFailWith(result, "Test.StoreFailure");
    }

    [Fact(DisplayName = "RevokeAsync should revoke and persist the token")]
    public async Task RevokeAsync_ShouldRevokeAndPersist()
    {
        var store = new FakeRefreshTokenStore();
        RefreshTokenService service = CreateService(store);
        RefreshTokenIssue issue = (await service.IssueAsync("user-1", Now)).Value;

        Result result = await service.RevokeAsync(
            issue.Entity,
            Now.AddMinutes(1),
            RefreshTokenRevocationReason.UserLogout);

        result.IsSuccess.Should().BeTrue();
        issue.Entity.IsRevoked.Should().BeTrue();
        issue.Entity.RevocationReason.Should().Be(RefreshTokenRevocationReason.UserLogout);
        store.UpdateCount.Should().Be(1);
    }

    [Fact(DisplayName = "RevokeAsync should fail for an undefined reason")]
    public async Task RevokeAsync_ShouldRejectInvalidReason()
    {
        var store = new FakeRefreshTokenStore();
        RefreshTokenService service = CreateService(store);
        RefreshTokenIssue issue = (await service.IssueAsync("user-1", Now)).Value;

        Result result = await service.RevokeAsync(
            issue.Entity,
            Now.AddMinutes(1),
            (RefreshTokenRevocationReason)99);

        ShouldFailWith(result, "RefreshToken.RevocationReason.Invalid");
        issue.Entity.IsRevoked.Should().BeFalse();
        store.UpdateCount.Should().Be(0);
    }

    [Fact(DisplayName = "RevokeAsync should reject a timestamp before creation")]
    public async Task RevokeAsync_ShouldRejectTimestampBeforeCreation()
    {
        var store = new FakeRefreshTokenStore();
        RefreshTokenService service = CreateService(store);
        RefreshTokenIssue issue = (await service.IssueAsync("user-1", Now)).Value;

        Result result = await service.RevokeAsync(
            issue.Entity,
            Now.AddMinutes(-1),
            RefreshTokenRevocationReason.UserLogout);

        ShouldFailWith(result, "RefreshToken.RevokedAt.BeforeCreated");
        issue.Entity.IsRevoked.Should().BeFalse();
    }

    [Fact(DisplayName = "RevokeAsync should propagate a store failure")]
    public async Task RevokeAsync_ShouldPropagateStoreFailure()
    {
        var store = new FakeRefreshTokenStore();
        RefreshTokenService service = CreateService(store);
        RefreshTokenIssue issue = (await service.IssueAsync("user-1", Now)).Value;
        store.FailUpdate = true;

        Result result = await service.RevokeAsync(
            issue.Entity,
            Now.AddMinutes(1),
            RefreshTokenRevocationReason.UserLogout);

        ShouldFailWith(result, "Test.StoreFailure");
    }

    [Fact(DisplayName = "MarkReuseAsync should record the theft reason")]
    public async Task MarkReuseAsync_ShouldRecordTheftReason()
    {
        var store = new FakeRefreshTokenStore();
        RefreshTokenService service = CreateService(store);
        RefreshTokenIssue issue = (await service.IssueAsync("user-1", Now)).Value;

        Result result = await service.MarkReuseAsync(issue.Entity, Now.AddMinutes(1));

        result.IsSuccess.Should().BeTrue();
        issue.Entity.RevocationReason.Should().Be(RefreshTokenRevocationReason.ReuseDetected);
        store.UpdateCount.Should().Be(1);
    }

    [Fact(DisplayName = "RotateAsync should create a replacement and revoke the current token")]
    public async Task RotateAsync_ShouldCreateReplacement()
    {
        var store = new FakeRefreshTokenStore();
        RefreshTokenService service = CreateService(store);
        RefreshTokenIssue issue = (await service.IssueAsync("user-1", Now)).Value;

        Result<RefreshTokenIssue> result = await service.RotateAsync(issue.Entity, Now.AddDays(1));

        result.IsSuccess.Should().BeTrue();
        RefreshTokenIssue replacement = result.Value;
        replacement.Token.Should().NotBe(issue.Token);
        replacement.TokenFamilyId.Should().Be(issue.TokenFamilyId);
        replacement.Entity.UserId.Should().Be("user-1");
        replacement.Entity.IsActive(Now.AddDays(1)).Should().BeTrue();
        issue.Entity.IsRevoked.Should().BeTrue();
        issue.Entity.RevocationReason.Should().Be(RefreshTokenRevocationReason.Replaced);
        issue.Entity.ReplacedByToken.Should().BeSameAs(replacement.Entity);
        store.RotateCount.Should().Be(1);
    }

    [Fact(DisplayName = "RotateAsync should reject an expired token")]
    public async Task RotateAsync_ShouldRejectExpiredToken()
    {
        var store = new FakeRefreshTokenStore();
        RefreshTokenService service = CreateService(store);
        RefreshTokenIssue issue = (await service.IssueAsync("user-1", Now)).Value;

        Result<RefreshTokenIssue> result = await service.RotateAsync(issue.Entity, Now.AddDays(8));

        ShouldFailWith(result, "RefreshToken.Expired");
        store.RotateCount.Should().Be(0);
    }

    [Fact(DisplayName = "RotateAsync should reject a revoked token")]
    public async Task RotateAsync_ShouldRejectRevokedToken()
    {
        var store = new FakeRefreshTokenStore();
        RefreshTokenService service = CreateService(store);
        RefreshTokenIssue issue = (await service.IssueAsync("user-1", Now)).Value;
        await service.RevokeAsync(issue.Entity, Now, RefreshTokenRevocationReason.UserLogout);

        Result<RefreshTokenIssue> result = await service.RotateAsync(issue.Entity, Now.AddDays(1));

        ShouldFailWith(result, "RefreshToken.Revoked");
    }

    [Fact(DisplayName = "RotateAsync should propagate a store failure")]
    public async Task RotateAsync_ShouldPropagateStoreFailure()
    {
        var store = new FakeRefreshTokenStore();
        RefreshTokenService service = CreateService(store);
        RefreshTokenIssue issue = (await service.IssueAsync("user-1", Now)).Value;
        store.FailRotate = true;

        Result<RefreshTokenIssue> result = await service.RotateAsync(issue.Entity, Now.AddDays(1));

        ShouldFailWith(result, "Test.StoreFailure");
    }

    [Fact(DisplayName = "RevokeAllAsync should revoke only the user's tokens")]
    public async Task RevokeAllAsync_ShouldRevokeOnlyUserTokens()
    {
        var store = new FakeRefreshTokenStore();
        RefreshTokenService service = CreateService(store);
        RefreshTokenIssue first = (await service.IssueAsync("user-1", Now)).Value;
        RefreshTokenIssue second = (await service.IssueAsync("user-1", Now)).Value;
        RefreshTokenIssue other = (await service.IssueAsync("user-2", Now)).Value;

        Result<int> result = await service.RevokeAllAsync(" user-1 ", Now.AddMinutes(1));

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(2);
        first.Entity.IsRevoked.Should().BeTrue();
        second.Entity.IsRevoked.Should().BeTrue();
        other.Entity.IsRevoked.Should().BeFalse();
        first.Entity.RevocationReason.Should().Be(RefreshTokenRevocationReason.UserLogoutAll);
        store.RevokeAllCount.Should().Be(1);
    }

    [Fact(DisplayName = "RevokeAllAsync should reject a missing user identifier")]
    public async Task RevokeAllAsync_ShouldRejectMissingUser()
    {
        var store = new FakeRefreshTokenStore();
        RefreshTokenService service = CreateService(store);

        Result<int> result = await service.RevokeAllAsync("   ", Now);

        ShouldFailWith(result, "Token.UserId.Required");
    }

    [Fact(DisplayName = "RevokeAllAsync should propagate a store failure")]
    public async Task RevokeAllAsync_ShouldPropagateStoreFailure()
    {
        var store = new FakeRefreshTokenStore { FailRevokeAll = true };
        RefreshTokenService service = CreateService(store);

        Result<int> result = await service.RevokeAllAsync("user-1", Now);

        ShouldFailWith(result, "Test.StoreFailure");
    }

    [Fact(DisplayName = "The constructor should validate its dependencies and settings")]
    public void Constructor_ShouldValidateDependenciesAndSettings()
    {
        Assert.Throws<ArgumentNullException>(() => new RefreshTokenService(null!, new RefreshTokenOption()));
        Assert.Throws<ArgumentNullException>(() => new RefreshTokenService(new FakeRefreshTokenStore(), null!));
        Assert.Throws<InvalidOperationException>(
            () => new RefreshTokenService(new FakeRefreshTokenStore(), new RefreshTokenOption { LifetimeDays = 0 }));
    }

    private sealed class FakeRefreshTokenStore : IRefreshTokenStore
    {
        public List<RefreshToken> Tokens { get; } = [];

        public bool FailFind { get; init; }

        public bool FailAdd { get; init; }

        public bool FailUpdate { get; set; }

        public bool FailRotate { get; set; }

        public bool FailRevokeAll { get; init; }

        public int AddCount { get; private set; }

        public int UpdateCount { get; private set; }

        public int RotateCount { get; private set; }

        public int RevokeAllCount { get; private set; }

        public Task<Result<Maybe<RefreshToken>>> FindByHashAsync(
            string tokenHash,
            CancellationToken cancellationToken = default)
        {
            if (FailFind)
                return Task.FromResult(Result<Maybe<RefreshToken>>.Fail(StoreError));

            RefreshToken? token = Tokens.FirstOrDefault(candidate => candidate.TokenHash == tokenHash);
            Maybe<RefreshToken> maybe = token is null
                ? Maybe<RefreshToken>.Empty()
                : Maybe<RefreshToken>.From(token);

            return Task.FromResult(Result<Maybe<RefreshToken>>.Ok(maybe));
        }

        public Task<Result> AddAsync(
            RefreshToken token,
            CancellationToken cancellationToken = default)
        {
            if (FailAdd)
                return Task.FromResult(Result.Fail(StoreError));

            Tokens.Add(token);
            AddCount++;
            return Task.FromResult(Result.Ok());
        }

        public Task<Result> UpdateAsync(
            RefreshToken token,
            CancellationToken cancellationToken = default)
        {
            if (FailUpdate)
                return Task.FromResult(Result.Fail(StoreError));

            UpdateCount++;
            return Task.FromResult(Result.Ok());
        }

        public Task<Result> RotateAsync(
            RefreshToken current,
            RefreshToken replacement,
            CancellationToken cancellationToken = default)
        {
            if (FailRotate)
                return Task.FromResult(Result.Fail(StoreError));

            Tokens.Add(replacement);
            RotateCount++;
            return Task.FromResult(Result.Ok());
        }

        public Task<Result<int>> RevokeAllAsync(
            string userId,
            DateTimeOffset revokedAtUtc,
            RefreshTokenRevocationReason reason,
            CancellationToken cancellationToken = default)
        {
            if (FailRevokeAll)
                return Task.FromResult(Result<int>.Fail(StoreError));

            List<RefreshToken> targets = [.. Tokens.Where(
                token => token.UserId == userId && !token.IsRevoked)];

            foreach (RefreshToken token in targets)
            {
                Result<RefreshToken>.Success(token).Revoke(revokedAtUtc, reason);
            }

            RevokeAllCount++;
            return Task.FromResult(Result<int>.Ok(targets.Count));
        }
    }
}
