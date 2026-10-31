namespace Application.BuildingBlocks.Caching;

/// <summary>Conventions shared by every application cache key.</summary>
public static class CachingConstant
{
    /// <summary>Key building conventions.</summary>
    public static class Key
    {
        /// <summary>Separator between the slice, the entity, and the identifier.</summary>
        public const char Separator = ':';

        /// <summary>Longest key the default configuration accepts.</summary>
        public const int MaxLength = 1024;
    }

    /// <summary>Tag building conventions.</summary>
    public static class Tag
    {
        /// <summary>Separator between a tag's scope and its identifier.</summary>
        public const char Separator = ':';
    }
}