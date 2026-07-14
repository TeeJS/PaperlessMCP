using FluentAssertions;
using Microsoft.AspNetCore.Http;
using PaperlessMCP.Utils;
using Xunit;

namespace PaperlessMCP.Tests.Utils;

public class McpAcceptHeaderCompatibilityTests
{
    [Fact]
    public void EnsureStreamableHttpAcceptHeader_WhenHeaderIsMissing_AddsRequiredMediaTypes()
    {
        var request = new DefaultHttpContext().Request;

        var changed = McpAcceptHeaderCompatibility.EnsureStreamableHttpAcceptHeader(request);

        changed.Should().BeTrue();
        request.Headers.Accept.ToString().Should().Be(McpAcceptHeaderCompatibility.RequiredAcceptHeader);
    }

    [Fact]
    public void EnsureStreamableHttpAcceptHeader_WhenHeaderOnlyAcceptsJson_AddsRequiredMediaTypes()
    {
        var request = new DefaultHttpContext().Request;
        request.Headers.Accept = "application/json";

        var changed = McpAcceptHeaderCompatibility.EnsureStreamableHttpAcceptHeader(request);

        changed.Should().BeTrue();
        request.Headers.Accept.ToString().Should().Be(McpAcceptHeaderCompatibility.RequiredAcceptHeader);
    }

    [Fact]
    public void EnsureStreamableHttpAcceptHeader_WhenHeaderAlreadyAcceptsBothMediaTypes_DoesNotChangeHeader()
    {
        var request = new DefaultHttpContext().Request;
        request.Headers.Accept = "application/json, text/event-stream";

        var changed = McpAcceptHeaderCompatibility.EnsureStreamableHttpAcceptHeader(request);

        changed.Should().BeFalse();
        request.Headers.Accept.ToString().Should().Be("application/json, text/event-stream");
    }
}
