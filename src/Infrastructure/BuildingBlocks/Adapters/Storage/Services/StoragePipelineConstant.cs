namespace Infrastructure.BuildingBlocks.Adapters.Storage.Services;

/// <summary>Pipeline metadata keys written by the storage service.</summary>
public static class StoragePipelineConstant
{
    /// <summary>Metadata keys attached to stored objects.</summary>
    public static class MetadataKeys
    {
        /// <summary>Content hash of the stored payload.</summary>
        public const string ContentHash = "content-hash";

        /// <summary>Threat reported by the malware scanner.</summary>
        public const string MalwareThreat = "malware-threat";

        /// <summary>Threat that caused quarantine.</summary>
        public const string QuarantineThreat = "quarantine-threat";

        /// <summary>Timestamp when the object was quarantined.</summary>
        public const string QuarantineTimestamp = "quarantine-timestamp";

        /// <summary>Marks an upload that replaced an existing object.</summary>
        public const string OverwriteExisting = "overwrite-existing";
    }
}
