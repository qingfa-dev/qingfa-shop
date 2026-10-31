namespace AppHost;

/// <summary>Resource names shared by the Aspire orchestration.</summary>
public static class AppHostConstant
{
    /// <summary>Named volumes pinned by the AppHost resources.</summary>
    public static class Volumes
    {
        /// <summary>RabbitMQ data volume.</summary>
        public const string RabbitMqData = "rabbitmq-data";
    }
}
