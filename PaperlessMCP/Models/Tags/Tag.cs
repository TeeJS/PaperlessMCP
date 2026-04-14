using System.Text.Json.Serialization;

namespace PaperlessMCP.Models.Tags;

/// <summary>
/// Represents a tag in Paperless-ngx.
/// </summary>
public record Tag
{
    [JsonPropertyName("id")]
    public int Id { get; init; }

    [JsonPropertyName("slug")]
    public string Slug { get; init; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    [JsonPropertyName("color")]
    public string? Color { get; init; }

    [JsonPropertyName("text_color")]
    public string? TextColor { get; init; }

    [JsonPropertyName("match")]
    public string? Match { get; init; }

    [JsonPropertyName("matching_algorithm")]
    public int MatchingAlgorithm { get; init; }

    [JsonPropertyName("is_inbox_tag")]
    public bool IsInboxTag { get; init; }

    [JsonPropertyName("document_count")]
    public int DocumentCount { get; init; }

    [JsonPropertyName("owner")]
    public int? Owner { get; init; }

    [JsonPropertyName("parent")]
    public int? Parent { get; init; }
}

/// <summary>
/// Request to create a new tag.
/// </summary>
public record TagCreateRequest
{
    [JsonPropertyName("name")]
    public required string Name { get; init; }

    [JsonPropertyName("color")]
    public string? Color { get; init; }

    [JsonPropertyName("match")]
    public string? Match { get; init; }

    [JsonPropertyName("matching_algorithm")]
    public int? MatchingAlgorithm { get; init; }

    [JsonPropertyName("is_inbox_tag")]
    public bool? IsInboxTag { get; init; }

    [JsonPropertyName("parent")]
    public int? Parent { get; init; }
}

/// <summary>
/// Request to update an existing tag.
/// </summary>
public record TagUpdateRequest
{
    [JsonPropertyName("name")]
    public string? Name { get; init; }

    [JsonPropertyName("color")]
    public string? Color { get; init; }

    [JsonPropertyName("match")]
    public string? Match { get; init; }

    [JsonPropertyName("matching_algorithm")]
    public int? MatchingAlgorithm { get; init; }

    [JsonPropertyName("is_inbox_tag")]
    public bool? IsInboxTag { get; init; }

    [JsonPropertyName("parent")]
    public int? Parent { get; init; }
}

/// <summary>
/// Matching algorithm types used by tags, correspondents, and document types.
/// </summary>
public static class MatchingAlgorithm
{
    public const int None = 0;
    public const int Any = 1;
    public const int All = 2;
    public const int Literal = 3;
    public const int Regex = 4;
    public const int Fuzzy = 5;
    public const int Auto = 6;
}
