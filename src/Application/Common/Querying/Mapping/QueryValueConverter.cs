namespace Application.Common.Querying.Mapping;

/// <summary>Converts raw query text to the mapped member type.</summary>
public delegate Result<QueryValue> QueryValueConverter(string value);