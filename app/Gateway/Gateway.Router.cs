using Yarp.ReverseProxy.Configuration;

namespace Gateway;

/// <summary>
/// The proxy's route and cluster table. Declared in code rather than configuration
/// because the ordering below is load-bearing and belongs next to its explanation.
/// </summary>
internal static class GatewayRouter
{
    /// <summary>Reserved by YARP: marks a route as explicitly anonymous.</summary>
    public const string AnonymousPolicy = "Anonymous";

    /// <summary>JWT policy for everything under <c>/api</c> except the time probe.</summary>
    public const string ProtectedApiPolicy = "EdgeApi";

    /// <summary>Rate-limiter policy name applied to every proxied request.</summary>
    public const string RateLimitPolicy = "edge";

    private const string ApiCluster = "api";
    private const string ShopFrontCluster = "shopfront";
    private const string AdminCluster = "admin";

    /// <summary>The ordered route table.</summary>
    /// <remarks>
    /// Order matters twice over: YARP evaluates routes as declared, so the narrow
    /// <c>/api/time</c> match must precede the <c>/api</c> catch-all, and the
    /// storefront catch-all must be last or it would swallow <c>/api</c> and
    /// <c>/admin</c>. <c>/alive</c> and <c>/health</c> are never matched here —
    /// they are ASP.NET endpoints with more specific patterns than a catch-all.
    /// </remarks>
    public static IReadOnlyList<RouteConfig> Routes() =>
    [
        // Validate: anonymous on purpose. The adopted gateway test calls /api/time
        // with no bearer token and asserts 200; this route keeps that true while the
        // catch-all below protects every other /api call.
        new RouteConfig
        {
            RouteId = GatewayConstant.Routes.ApiTime,
            ClusterId = ApiCluster,
            Match = new RouteMatch { Path = "/api/time" },
            Transforms = StripPrefix("/time"),
            AuthorizationPolicy = AnonymousPolicy,
        },
        // Validate: providers cannot present a bearer token, and the catch-all would
        // strip /api from a path the Api serves verbatim.
        new RouteConfig
        {
            RouteId = GatewayConstant.Routes.ApiPaymentsWebhook,
            ClusterId = ApiCluster,
            Match = new RouteMatch { Path = "/api/payments/webhooks/{**remainder}" },
            AuthorizationPolicy = AnonymousPolicy,
        },
        new RouteConfig
        {
            RouteId = GatewayConstant.Routes.ApiPaymentsAdmin,
            ClusterId = ApiCluster,
            Match = new RouteMatch { Path = "/api/admin/payments/{**remainder}" },
            AuthorizationPolicy = ProtectedApiPolicy,
        },
        new RouteConfig
        {
            RouteId = GatewayConstant.Routes.ApiPayments,
            ClusterId = ApiCluster,
            Match = new RouteMatch { Path = "/api/payments/{**remainder}" },
            AuthorizationPolicy = ProtectedApiPolicy,
        },
        new RouteConfig
        {
            RouteId = "api",
            ClusterId = ApiCluster,
            Match = new RouteMatch { Path = "/api/{**remainder}" },
            Transforms = StripPrefix("/{remainder}"),
            AuthorizationPolicy = ProtectedApiPolicy,
        },
        new RouteConfig
        {
            RouteId = "admin",
            ClusterId = AdminCluster,
            Match = new RouteMatch { Path = "/admin/{**remainder}" },

            // Handle: no path transform. The Admin SPA is built with base '/admin/',
            // so it must receive its own mount point verbatim — including the trailing
            // slash on '/admin/', which a PathPattern rewrite drops.
        },
        new RouteConfig
        {
            RouteId = "shopfront",
            ClusterId = ShopFrontCluster,
            Match = new RouteMatch { Path = "/{**remainder}" },
            Transforms = StripPrefix("/{remainder}"),
        },
    ];

    /// <summary>
    /// The cluster table. Addresses default to Aspire service-discovery names and can be
    /// overridden per environment; Compose uses api:8080 because the .NET image runs
    /// non-root and cannot bind port 80.
    /// </summary>
    public static IReadOnlyList<ClusterConfig> Clusters(IConfiguration configuration) =>
    [
        Cluster(ApiCluster, Address(configuration, ApiCluster, "http://api")),
        Cluster(ShopFrontCluster, Address(configuration, ShopFrontCluster, "http://shopfront")),
        Cluster(AdminCluster, Address(configuration, AdminCluster, "http://admin")),
    ];

    private static string Address(IConfiguration configuration, string name, string fallback) =>
        configuration[$"Gateway:Clusters:{name}"] ?? fallback;

    private static ClusterConfig Cluster(string id, string address) => new()
    {
        ClusterId = id,
        Destinations = new Dictionary<string, DestinationConfig>
        {
            [id] = new DestinationConfig { Address = address },
        },
    };

    private static IReadOnlyList<IReadOnlyDictionary<string, string>> StripPrefix(string pathPattern) =>
        [new Dictionary<string, string> { ["PathPattern"] = pathPattern }];
}