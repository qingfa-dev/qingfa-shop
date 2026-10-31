namespace Gateway;

/// <summary>Edge limits, route identities, and header names.</summary>
public static class GatewayConstant
{
    /// <summary>Fixed-window rate limiting applied to every proxied request.</summary>
    public static class RateLimiting
    {
        /// <summary>Permitted requests per window.</summary>
        public const int PermitLimit = 100;

        /// <summary>Window length in seconds.</summary>
        public const int WindowSeconds = 10;
    }

    /// <summary>YARP route identifiers; declaration order is load-bearing.</summary>
    public static class Routes
    {
        /// <summary>Anonymous time probe.</summary>
        public const string ApiTime = "api-time";

        /// <summary>Anonymous payment webhooks.</summary>
        public const string ApiPaymentsWebhook = "api-payments-webhook";

        /// <summary>Admin payment routes.</summary>
        public const string ApiPaymentsAdmin = "api-payments-admin";

        /// <summary>Customer payment routes.</summary>
        public const string ApiPayments = "api-payments";
    }

    /// <summary>Header names exchanged at the edge.</summary>
    public static class Headers
    {
        /// <summary>Correlation id echoed by the gateway middleware.</summary>
        public const string CorrelationId = "X-Correlation-ID";
    }
}
