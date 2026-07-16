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
    public DocumentNoteUser? User { get; init; }
}

/// <summary>
/// User attached to a document note. Paperless API v8+ returns an object,
/// while older API versions return only the numeric user ID.
/// </summary>
[JsonConverter(typeof(DocumentNoteUserJsonConverter))]
public record DocumentNoteUser
{
    [JsonPropertyName("id")]
    public int Id { get; init; }

    [JsonPropertyName("username")]
    public string Username { get; init; } = string.Empty;

    [JsonPropertyName("first_name")]
    public string FirstName { get; init; } = string.Empty;

    [JsonPropertyName("last_name")]
    public string LastName { get; init; } = string.Empty;

    internal bool IsLegacyIdOnly { get; init; }
}

/// <summary>
/// Reads both Paperless note-user response shapes and preserves their shape
/// when the model is serialized again.
/// </summary>
public sealed class DocumentNoteUserJsonConverter : JsonConverter<DocumentNoteUser>
{
    public override DocumentNoteUser Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Number)
        {
            return new DocumentNoteUser
            {
                Id = reader.GetInt32(),
                IsLegacyIdOnly = true
            };
        }

        if (reader.TokenType != JsonTokenType.StartObject)
        {
            throw new JsonException("Document note user must be a numeric ID or an object.");
        }

        using var document = JsonDocument.ParseValue(ref reader);
        var root = document.RootElement;
        if (!root.TryGetProperty("id", out var idElement) || !idElement.TryGetInt32(out var id))
        {
            throw new JsonException("Document note user object must contain a numeric ID.");
        }

        return new DocumentNoteUser
        {
            Id = id,
            Username = GetString(root, "username"),
            FirstName = GetString(root, "first_name"),
            LastName = GetString(root, "last_name")
        };
    }

    public override void Write(
        Utf8JsonWriter writer,
        DocumentNoteUser value,
        JsonSerializerOptions options)
    {
        if (value.IsLegacyIdOnly)
        {
            writer.WriteNumberValue(value.Id);
            return;
        }

        writer.WriteStartObject();
        writer.WriteNumber("id", value.Id);
        writer.WriteString("username", value.Username);
        writer.WriteString("first_name", value.FirstName);
        writer.WriteString("last_name", value.LastName);
        writer.WriteEndObject();
    }

    private static string GetString(JsonElement root, string propertyName)
    {
        return root.TryGetProperty(propertyName, out var element) && element.ValueKind == JsonValueKind.String
            ? element.GetString() ?? string.Empty
            : string.Empty;
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
