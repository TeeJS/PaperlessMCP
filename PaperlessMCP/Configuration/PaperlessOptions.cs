namespace PaperlessMCP.Configuration;

/// <summary>
/// Configuration options for connecting to the Paperless-ngx API.
/// </summary>
public class PaperlessOptions
{
    /// <summary>
    /// Base URL of the Paperless-ngx instance (e.g., https://docs.example.com).
    /// </summary>
    public string BaseUrl { get; set; } = string.Empty;

    /// <summary>
    /// API token for authentication.
    /// </summary>
    public string ApiToken { get; set; } = string.Empty;

    /// <summary>
    /// Maximum page size for paginated requests.
    /// </summary>
    public int MaxPageSize { get; set; } = 100;

    /// <summary>
    /// HTTP request timeout in seconds for calls to the Paperless-ngx API.
    /// Large full-text searches over big libraries can exceed the default.
    /// </summary>
    public int HttpTimeoutSeconds { get; set; } = 30;
}
