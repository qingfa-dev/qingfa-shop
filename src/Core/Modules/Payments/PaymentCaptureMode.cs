namespace Core.Modules.Payments;

/// <summary>When the provider moves the money.</summary>
public enum PaymentCaptureMode
{
    /// <summary>Authorize and capture in one provider call.</summary>
    Automatic = 0,

    /// <summary>Authorize now and capture later through an admin command.</summary>
    Manual = 1
}