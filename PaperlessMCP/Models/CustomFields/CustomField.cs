using System.Text.Json;
using System.Text.Json.Serialization;

namespace PaperlessMCP.Models.CustomFields;

/// <summary>
/// Represents a custom field definition in Paperless-ngx.
/// </summary>
public record CustomField
{
    [JsonPropertyName("id")]
    public int Id { get; init; }

    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    [JsonPropertyName("data_type")]
    public string DataType { get; init; } = string.Empty;

    [JsonPropertyName("extra_data")]
    public CustomFieldExtraData? ExtraData { get; init; }
}

/// <summary>
/// A single option in a select-type custom field.
///
/// Paperless-ngx v2.14+ requires each option serialised as
/// {"label": "..."} rather than a bare string. The old bare-string
/// form makes paperless's serialisers.py raise
/// "'str' object has no attribute 'get'", surfaced as a generic
/// UPSTREAM_ERROR / 400 through this MCP.
/// </summary>
[JsonConverter(typeof(SelectOptionJsonConverter))]
public record SelectOption
{
    [JsonPropertyName("id")]
    public string? Id { get; init; }

    [JsonPropertyName("label")]
    public required string Label { get; init; }
}

/// <summary>
/// Accepts both the string options returned by Paperless-ngx before v2.14 and
/// the object options returned by v2.14 and later. Responses are normalized to
/// the object shape so option IDs remain available to MCP clients.
/// </summary>
public sealed class SelectOptionJsonConverter : JsonConverter<SelectOption>
{
    public override SelectOption Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            return new SelectOption { Label = reader.GetString() ?? string.Empty };
        }

        if (reader.TokenType != JsonTokenType.StartObject)
        {
            throw new JsonException("Select option must be a string or an object.");
        }

        using var document = JsonDocument.ParseValue(ref reader);
        var root = document.RootElement;

        if (!root.TryGetProperty("label", out var labelElement) || labelElement.ValueKind != JsonValueKind.String)
        {
            throw new JsonException("Select option object must contain a string label.");
        }

        string? id = null;
        if (root.TryGetProperty("id", out var idElement) && idElement.ValueKind == JsonValueKind.String)
        {
            id = idElement.GetString();
        }

        return new SelectOption
        {
            Id = id,
            Label = labelElement.GetString() ?? string.Empty
        };
    }

    public override void Write(Utf8JsonWriter writer, SelectOption value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        if (value.Id != null)
        {
            writer.WriteString("id", value.Id);
        }

        writer.WriteString("label", value.Label);
        writer.WriteEndObject();
    }
}

/// <summary>
/// Extra data for select-type custom fields.
/// </summary>
public record CustomFieldExtraData
{
    [JsonPropertyName("select_options")]
    public List<SelectOption>? SelectOptions { get; init; }

    [JsonPropertyName("default_currency")]
    public string? DefaultCurrency { get; init; }
}

/// <summary>
/// Request to create a new custom field.
/// </summary>
public record CustomFieldCreateRequest
{
    [JsonPropertyName("name")]
    public required string Name { get; init; }

    [JsonPropertyName("data_type")]
    public required string DataType { get; init; }

    [JsonPropertyName("extra_data")]
    public CustomFieldExtraData? ExtraData { get; init; }
}

/// <summary>
/// Request to update an existing custom field.
/// </summary>
public record CustomFieldUpdateRequest
{
    [JsonPropertyName("name")]
    public string? Name { get; init; }

    [JsonPropertyName("extra_data")]
    public CustomFieldExtraData? ExtraData { get; init; }
}

/// <summary>
/// Custom field data types.
/// </summary>
public static class CustomFieldDataType
{
    public const string String = "string";
    public const string Url = "url";
    public const string Date = "date";
    public const string Boolean = "boolean";
    public const string Integer = "integer";
    public const string Float = "float";
    public const string Monetary = "monetary";
    public const string DocumentLink = "documentlink";
    public const string Select = "select";
}

/// <summary>
/// Request to assign a custom field value to a document.
/// </summary>
public record CustomFieldAssignRequest
{
    [JsonPropertyName("document_id")]
    public required int DocumentId { get; init; }

    [JsonPropertyName("field_id")]
    public required int FieldId { get; init; }

    [JsonPropertyName("value")]
    public object? Value { get; init; }
}
