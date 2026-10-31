namespace Infrastructure.Modules.Payments.Adapters.Fake;

/// <summary>Values the fake provider scripts and signs with.</summary>
public static class FakePaymentConstant
{
    /// <summary>The shared secret mixed into the signature digest.</summary>
    public const string WebhookSecret = "fake-webhook-secret";

    /// <summary>The failure code reported for scripted declines.</summary>
    public const string DeclinedCode = "fake_declined";

    /// <summary>The prefix of the SHA-256 digest signature.</summary>
    public const string SignaturePrefix = "sha256=";

    /// <summary>Scripts the fake provider to decline.</summary>
    public const string DeclinePrefix = "decline:";

    /// <summary>Scripts the fake provider to time out.</summary>
    public const string TimeoutPrefix = "timeout:";

    /// <summary>Scripts the fake provider to report processing before a later decline.</summary>
    public const string DeclinedAsyncPrefix = "declined-async:";

    /// <summary>How long a fake intent claims to stay payable.</summary>
    public const int IntentLifetimeMinutes = 30;
}
