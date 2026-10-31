namespace Core.Modules.Payments.Disputes;

/// <summary>The lifecycle state of a provider dispute.</summary>
public enum DisputeStatus
{
    /// <summary>The provider opened a dispute and is reviewing it.</summary>
    Opened = 0,

    /// <summary>The merchant won the dispute.</summary>
    Won = 1,

    /// <summary>The merchant lost the dispute; funds are withdrawn by the provider.</summary>
    Lost = 2,

    /// <summary>The merchant accepted the dispute.</summary>
    Accepted = 3
}

/// <summary>The outcome of a resolved dispute.</summary>
public enum DisputeOutcome
{
    /// <summary>The merchant won.</summary>
    Won = 0,

    /// <summary>The merchant lost.</summary>
    Lost = 1,

    /// <summary>The merchant accepted the dispute.</summary>
    Accepted = 2
}

/// <summary>Converts dispute statuses to their wire values.</summary>
public static class DisputeStatusExtension
{
    /// <summary>Returns the lowercase wire value.</summary>
    public static string ToWire(this DisputeStatus status) => status.ToString().ToLowerInvariant();
}
