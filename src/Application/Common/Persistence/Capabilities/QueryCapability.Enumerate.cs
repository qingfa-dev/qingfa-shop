namespace Application.Common.Persistence.Capabilities;

[Flags]
public enum SearchModeSupport
{
    None = 0,
    Contains = 1 << 0,
    StartsWith = 1 << 1,
    Exact = 1 << 2,
    FullText = 1 << 3,
    Fuzzy = 1 << 4,
    All = Contains | StartsWith | Exact | FullText | Fuzzy,
}