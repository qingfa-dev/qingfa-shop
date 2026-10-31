using AwesomeAssertions;

using Infrastructure.Modules.Payments;
using Infrastructure.Payments.Configuration;

using Microsoft.Extensions.Options;

namespace Infrastructure.Unit.Tests.Modules.Payments;

/// <summary>Validates the per-currency payment policy configuration.</summary>
public sealed class PaymentPolicyOptionsValidatorTest
{
    private static readonly PaymentPolicyOptionsValidator Validator = new();

    [Fact]
    public void Validate_DefaultsWithoutCurrency_ShouldFail()
        => AssertFailed(new PaymentPolicyOptions());

    [Fact]
    public void Validate_ValidPolicy_ShouldSucceed()
    {
        var options = new PaymentPolicyOptions
        {
            MaximumAmountsByCurrency = new Dictionary<string, decimal> { ["USD"] = 1000m, ["EUR"] = 900m },
            HostedCheckoutIntentLifetime = TimeSpan.FromMinutes(30),
            ProviderExpiryGracePeriod = TimeSpan.FromMinutes(2)
        };

        ValidateOptionsResult result = Validator.Validate(null, options);

        result.Failed.Should().BeFalse();
    }

    [Theory]
    [InlineData("usd")]
    [InlineData("US")]
    [InlineData("USDD")]
    [InlineData("")]
    public void Validate_NonIsoCurrencyKey_ShouldFail(string currency)
    {
        var options = Valid();
        options.MaximumAmountsByCurrency = new Dictionary<string, decimal> { [currency] = 10m };

        AssertFailed(options);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_NonPositiveLimit_ShouldFail(decimal limit)
    {
        var options = Valid();
        options.MaximumAmountsByCurrency = new Dictionary<string, decimal> { ["USD"] = limit };

        AssertFailed(options);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(30)]
    [InlineData(97_200)]
    public void Validate_LifetimeOutOfRange_ShouldFail(int seconds)
    {
        var options = Valid();
        options.HostedCheckoutIntentLifetime = TimeSpan.FromSeconds(seconds);

        AssertFailed(options);
    }

    [Fact]
    public void Validate_NegativeGrace_ShouldFail()
    {
        var options = Valid();
        options.ProviderExpiryGracePeriod = TimeSpan.FromSeconds(-1);

        AssertFailed(options);
    }

    [Fact]
    public void Validate_GraceAboveTheCeiling_ShouldFail()
    {
        var options = Valid();
        options.ProviderExpiryGracePeriod = TimeSpan.FromHours(2);

        AssertFailed(options);
    }

    [Fact]
    public void Policy_ShouldExposeTheConfiguredValues()
    {
        var options = new PaymentPolicyOptions
        {
            MaximumAmountsByCurrency = new Dictionary<string, decimal> { ["USD"] = 250m },
            HostedCheckoutIntentLifetime = TimeSpan.FromMinutes(10),
            ProviderExpiryGracePeriod = TimeSpan.FromSeconds(30)
        };

        var policy = new PaymentPolicy(Options.Create(options));

        policy.MaximumAmountsByCurrency["usd"].Should().Be(250m);
        policy.HostedCheckoutIntentLifetime.Should().Be(TimeSpan.FromMinutes(10));
        policy.ProviderExpiryGracePeriod.Should().Be(TimeSpan.FromSeconds(30));
    }

    [Fact]
    public void Policy_NullOptions_ShouldThrow()
    {
        Action act = () => _ = new PaymentPolicy(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    /// <summary>Builds a valid policy.</summary>
    private static PaymentPolicyOptions Valid() => new()
    {
        MaximumAmountsByCurrency = new Dictionary<string, decimal> { ["USD"] = 1000m },
        HostedCheckoutIntentLifetime = TimeSpan.FromMinutes(30),
        ProviderExpiryGracePeriod = TimeSpan.FromMinutes(2)
    };

    /// <summary>Asserts the policy is rejected.</summary>
    private static void AssertFailed(PaymentPolicyOptions options)
    {
        ValidateOptionsResult result = Validator.Validate(null, options);

        result.Failed.Should().BeTrue();
    }
}
