using System.Text.Json;
using System.Text.Json.Serialization;

namespace PaperlessMCP.Models.Documents;

/// <summary>
/// Represents a document in Paperless-ngx.
/// </summary>
public record Document
{
    [JsonPropertyName("id")]
    public int Id { get; init; }

    [JsonPropertyName("correspondent")]
    public int? Correspondent { get; init; }

    [JsonPropertyName("document_type")]
    public int? DocumentType { get; init; }

    [JsonPropertyName("storage_path")]
    public int? StoragePath { get; init; }

    [JsonPropertyName("title")]
    public string Title { get; init; } = string.Empty;

    [JsonPropertyName("content")]
    public string Content { get; init; } = string.Empty;

    [JsonPropertyName("tags")]
    public List<int> Tags { get; init; } = [];

    [JsonPropertyName("created")]
    public DateTime? Created { get; init; }

    [JsonPropertyName("created_date")]
    public DateOnly? CreatedDate { get; init; }

    [JsonPropertyName("modified")]
    public DateTime? Modified { get; init; }

    [JsonPropertyName("added")]
    public DateTime? Added { get; init; }

    [JsonPropertyName("archive_serial_number")]
    public int? ArchiveSerialNumber { get; init; }

    [JsonPropertyName("original_file_name")]
    public string? OriginalFileName { get; init; }

    [JsonPropertyName("archived_file_name")]
    public string? ArchivedFileName { get; init; }

    [JsonPropertyName("owner")]
    public int? Owner { get; init; }

    [JsonPropertyName("custom_fields")]
    public List<DocumentCustomField> CustomFields { get; init; } = [];

    [JsonPropertyName("notes")]
    public List<DocumentNote>? Notes { get; init; }
}

/// <summary>
/// Custom field value assigned to a document.
/// </summary>
public record DocumentCustomField
{
    [JsonPropertyName("field")]
    public int Field { get; init; }

    [JsonPropertyName("value")]
    public object? Value { get; init; }
}

/// <summary>
/// Note attached to a document.
/// </summary>
public record DocumentNote
{
    [JsonPropertyName("id")]
    public int Id { get; init; }

    [JsonPropertyName("note")]
    public string Note { get; init; } = string.Empty;

    [JsonPropertyName("created")]
    public DateTime Created { get; init; }

    [JsonPropertyName("user")]
    [JsonConverter(typeof(FlexibleUserIdConverter))]
    public int? User { get; init; }
}

/// <summary>
/// Reads a "user" field that Paperless-ngx may serialize either as a bare user
/// id (older API) or as an expanded user object {"id": N, ...} (Paperless 2.x).
/// In both cases the user id is returned; anything else yields null. This keeps
/// the model tolerant of API version drift instead of failing the whole
/// deserialization (which the client would otherwise swallow into an empty result).
/// </summary>
public class FlexibleUserIdConverter : JsonConverter<int?>
{
    public override int? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        switch (reader.TokenType)
        {
            case JsonTokenType.Null:
                return null;
            case JsonTokenType.Number:
                return reader.GetInt32();
            case JsonTokenType.StartObject:
                using (var doc = JsonDocument.ParseValue(ref reader))
                {
                    return doc.RootElement.TryGetProperty("id", out var idProp)
                           && idProp.TryGetInt32(out var id)
                        ? id
                        : null;
                }
            default:
                reader.Skip();
                return null;
        }
    }

    public override void Write(Utf8JsonWriter writer, int? value, JsonSerializerOptions options)
    {
        if (value.HasValue)
            writer.WriteNumberValue(value.Value);
        else
            writer.WriteNullValue();
    }
}

/// <summary>
/// Search hit information returned with search results.
/// </summary>
public record SearchHit
{
    [JsonPropertyName("score")]
    public double? Score { get; init; }

    [JsonPropertyName("highlights")]
    public string? Highlights { get; init; }

    [JsonPropertyName("rank")]
    public int? Rank { get; init; }
}

/// <summary>
/// Document with search hit information.
/// </summary>
public record DocumentSearchResult : Document
{
    [JsonPropertyName("__search_hit__")]
    public SearchHit? SearchHit { get; init; }
}

/// <summary>
/// Lightweight document summary for search results.
/// Excludes full content and notes to reduce response size.
/// </summary>
public record DocumentSummary
{
    [JsonPropertyName("id")]
    public int Id { get; init; }

    [JsonPropertyName("correspondent")]
    public int? Correspondent { get; init; }

    [JsonPropertyName("document_type")]
    public int? DocumentType { get; init; }

    [JsonPropertyName("storage_path")]
    public int? StoragePath { get; init; }

    [JsonPropertyName("title")]
    public string Title { get; init; } = string.Empty;

    [JsonPropertyName("content")]
    public string? Content { get; init; }

    [JsonPropertyName("tags")]
    public List<int> Tags { get; init; } = [];

    [JsonPropertyName("created")]
    public DateTime? Created { get; init; }

    [JsonPropertyName("modified")]
    public DateTime? Modified { get; init; }

    [JsonPropertyName("added")]
    public DateTime? Added { get; init; }

    [JsonPropertyName("archive_serial_number")]
    public int? ArchiveSerialNumber { get; init; }

    [JsonPropertyName("original_file_name")]
    public string? OriginalFileName { get; init; }

    [JsonPropertyName("__search_hit__")]
    public SearchHit? SearchHit { get; init; }

    /// <summary>
    /// Creates a DocumentSummary from a DocumentSearchResult.
    /// </summary>
    public static DocumentSummary FromSearchResult(DocumentSearchResult result, bool includeContent = false, int? contentMaxLength = null)
    {
        string? content = null;
        if (includeContent && !string.IsNullOrEmpty(result.Content))
        {
            content = contentMaxLength.HasValue && result.Content.Length > contentMaxLength.Value
                ? result.Content[..contentMaxLength.Value] + "..."
                : result.Content;
        }

        return new DocumentSummary
        {
            Id = result.Id,
            Correspondent = result.Correspondent,
            DocumentType = result.DocumentType,
            StoragePath = result.StoragePath,
            Title = result.Title,
            Content = content,
            Tags = result.Tags,
            Created = result.Created,
            Modified = result.Modified,
            Added = result.Added,
            ArchiveSerialNumber = result.ArchiveSerialNumber,
            OriginalFileName = result.OriginalFileName,
            SearchHit = result.SearchHit
        };
    }
}

/// <summary>
/// Request to upload a new document.
/// </summary>
public record DocumentUploadRequest
{
    [JsonPropertyName("title")]
    public string? Title { get; init; }

    [JsonPropertyName("created")]
    public DateTime? Created { get; init; }

    [JsonPropertyName("correspondent")]
    public int? Correspondent { get; init; }

    [JsonPropertyName("document_type")]
    public int? DocumentType { get; init; }

    [JsonPropertyName("storage_path")]
    public int? StoragePath { get; init; }

    [JsonPropertyName("tags")]
    public List<int>? Tags { get; init; }

    [JsonPropertyName("archive_serial_number")]
    public int? ArchiveSerialNumber { get; init; }

    [JsonPropertyName("custom_fields")]
    public List<DocumentCustomField>? CustomFields { get; init; }
}

/// <summary>
/// Request to update an existing document.
/// </summary>
public record DocumentUpdateRequest
{
    [JsonPropertyName("title")]
    public string? Title { get; init; }

    [JsonPropertyName("correspondent")]
    public int? Correspondent { get; init; }

    [JsonPropertyName("document_type")]
    public int? DocumentType { get; init; }

    [JsonPropertyName("storage_path")]
    public int? StoragePath { get; init; }

    [JsonPropertyName("tags")]
    public List<int>? Tags { get; init; }

    [JsonPropertyName("archive_serial_number")]
    public int? ArchiveSerialNumber { get; init; }

    [JsonPropertyName("custom_fields")]
    public List<DocumentCustomField>? CustomFields { get; init; }

    [JsonPropertyName("created")]
    public DateTime? Created { get; init; }
}

/// <summary>
/// Download information for a document.
/// </summary>
public record DocumentDownload
{
    [JsonPropertyName("id")]
    public int Id { get; init; }

    [JsonPropertyName("title")]
    public string Title { get; init; } = string.Empty;

    [JsonPropertyName("original_file_name")]
    public string? OriginalFileName { get; init; }

    [JsonPropertyName("download_url")]
    public string DownloadUrl { get; init; } = string.Empty;

    [JsonPropertyName("preview_url")]
    public string? PreviewUrl { get; init; }

    [JsonPropertyName("thumbnail_url")]
    public string? ThumbnailUrl { get; init; }
}
