using FluentValidation.TestHelper;

using Infrastructure.Integrations.Http.Options;

namespace Infrastructure.Unit.Tests.Integrations.Http;

[Trait("Category", "Unit")]
[Trait("Feature", "Http")]
public class HttpOptionsValidatorTests
{
    private readonly HttpOptionsValidator _sut = new();

    [Fact(DisplayName = "When DefaultTimeoutSeconds less than 1 should fail")]
    public void WhenDefaultTimeoutSecondsLessThan1_ShouldFail()
    {
        var options = new HttpOptions { DefaultTimeoutSeconds = 0 };

        var result = _sut.TestValidate(options);

        result.ShouldHaveValidationErrorFor(x => x.DefaultTimeoutSeconds);
    }

    [Fact(DisplayName = "When DefaultTimeoutSeconds greater than 300 should fail")]
    public void WhenDefaultTimeoutSecondsGreaterThan300_ShouldFail()
    {
        var options = new HttpOptions { DefaultTimeoutSeconds = 301 };

        var result = _sut.TestValidate(options);

        result.ShouldHaveValidationErrorFor(x => x.DefaultTimeoutSeconds);
    }

    [Fact(DisplayName = "When DefaultTimeoutSeconds valid should pass")]
    public void WhenDefaultTimeoutSecondsValid_ShouldPass()
    {
        var options = new HttpOptions { DefaultTimeoutSeconds = 30 };

        var result = _sut.TestValidate(options);

        result.ShouldNotHaveValidationErrorFor(x => x.DefaultTimeoutSeconds);
    }

    [Fact(DisplayName = "When client BaseAddress empty should fail")]
    public void WhenClientBaseAddressEmpty_ShouldFail()
    {
        var options = new HttpOptions
        {
            Clients = new Dictionary<string, NamedClientOptions>
            {
                ["test"] = new() { BaseAddress = string.Empty }
            }
        };

        var result = _sut.TestValidate(options);

        result.ShouldHaveValidationErrorFor("Clients[0].Value.BaseAddress");
    }

    [Fact(DisplayName = "When client BaseAddress invalid URI should fail")]
    public void WhenClientBaseAddressInvalidUri_ShouldFail()
    {
        var options = new HttpOptions
        {
            Clients = new Dictionary<string, NamedClientOptions>
            {
                ["test"] = new() { BaseAddress = "not-a-uri" }
            }
        };

        var result = _sut.TestValidate(options);

        result.ShouldHaveValidationErrorFor("Clients[0].Value.BaseAddress");
    }

    [Fact(DisplayName = "When client BaseAddress valid should pass")]
    public void WhenClientBaseAddressValid_ShouldPass()
    {
        var options = new HttpOptions
        {
            Clients = new Dictionary<string, NamedClientOptions>
            {
                ["test"] = new() { BaseAddress = "https://api.example.com" }
            }
        };

        var result = _sut.TestValidate(options);

        result.ShouldNotHaveValidationErrorFor("Clients[0].Value.BaseAddress");
    }

    [Fact(DisplayName = "When client TimeoutSeconds negative should fail")]
    public void WhenClientTimeoutSecondsNegative_ShouldFail()
    {
        var options = new HttpOptions
        {
            Clients = new Dictionary<string, NamedClientOptions>
            {
                ["test"] = new()
                {
                    BaseAddress = "https://api.example.com",
                    TimeoutSeconds = -1
                }
            }
        };

        var result = _sut.TestValidate(options);

        result.ShouldHaveValidationErrorFor("Clients[0].Value.TimeoutSeconds");
    }

    [Fact(DisplayName = "When client TimeoutSeconds zero should pass")]
    public void WhenClientTimeoutSecondsZero_ShouldPass()
    {
        var options = new HttpOptions
        {
            Clients = new Dictionary<string, NamedClientOptions>
            {
                ["test"] = new()
                {
                    BaseAddress = "https://api.example.com",
                    TimeoutSeconds = 0
                }
            }
        };

        var result = _sut.TestValidate(options);

        result.ShouldNotHaveValidationErrorFor("Clients[0].Value.TimeoutSeconds");
    }

    [Fact(DisplayName = "When all valid should pass")]
    public void WhenAllValid_ShouldPass()
    {
        var options = new HttpOptions
        {
            DefaultTimeoutSeconds = 60,
            Clients = new Dictionary<string, NamedClientOptions>
            {
                ["api"] = new()
                {
                    BaseAddress = "https://api.example.com",
                    TimeoutSeconds = 30,
                    DefaultHeaders = new Dictionary<string, string>
                    {
                        ["User-Agent"] = "ReSys.Shop"
                    }
                }
            }
        };

        var result = _sut.TestValidate(options);

        result.ShouldNotHaveAnyValidationErrors();
    }
}
