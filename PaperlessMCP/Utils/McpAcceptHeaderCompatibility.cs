using Microsoft.AspNetCore.Http;
using Microsoft.Net.Http.Headers;

namespace PaperlessMCP.Utils;

internal static class McpAcceptHeaderCompatibility
{
    public const string RequiredAcceptHeader = "application/json, text/event-stream";

    public static bool EnsureStreamableHttpAcceptHeader(HttpRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var acceptHeader = request.GetTypedHeaders().Accept;
        if (acceptHeader.Any(MatchesApplicationJsonMediaType) &&
            acceptHeader.Any(MatchesTextEventStreamMediaType))
        {
            return false;
        }

        request.Headers.Accept = RequiredAcceptHeader;
        return true;
    }

    private static bool MatchesApplicationJsonMediaType(MediaTypeHeaderValue acceptHeaderValue) =>
        acceptHeaderValue.MatchesMediaType("application/json");

    private static bool MatchesTextEventStreamMediaType(MediaTypeHeaderValue acceptHeaderValue) =>
        acceptHeaderValue.MatchesMediaType("text/event-stream");
}
