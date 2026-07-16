using System.Net;
using System.Text.Json;
using FluentAssertions;
using PaperlessMCP.Tests.Fixtures;
using PaperlessMCP.Tools;
using RichardSzalay.MockHttp;
using Xunit;

namespace PaperlessMCP.Tests.Tools;

public class PaginationToolsTests : IDisposable
{
    private const string EmptyPage = """
        {
          "count": 0,
          "next": null,
          "previous": null,
          "results": []
        }
        """;

    private readonly MockHttpClientFactory _factory = new();

    public void Dispose()
    {
        _factory.Dispose();
    }

    [Theory]
    [InlineData("documents", "/api/documents/")]
    [InlineData("tags", "/api/tags/")]
    [InlineData("correspondents", "/api/correspondents/")]
    [InlineData("document_types", "/api/document_types/")]
    [InlineData("storage_paths", "/api/storage_paths/")]
    [InlineData("custom_fields", "/api/custom_fields/")]
    public async Task ListAndSearchTools_RespectConfiguredMaxPageSize(string tool, string expectedPath)
    {
        _factory.Options.MaxPageSize = 10;
        string? requestedPathAndQuery = null;

        _factory.MockHandler
            .When(HttpMethod.Get, "https://paperless.example.com/api/*")
            .With(request =>
            {
                requestedPathAndQuery = request.RequestUri?.PathAndQuery;
                return true;
            })
            .Respond("application/json", EmptyPage);

        var result = tool switch
        {
            "documents" => await DocumentTools.Search(_factory.Client, page: 2, pageSize: 50),
            "tags" => await TagTools.List(_factory.Client, page: 2, pageSize: 50),
            "correspondents" => await CorrespondentTools.List(_factory.Client, page: 2, pageSize: 50),
            "document_types" => await DocumentTypeTools.List(_factory.Client, page: 2, pageSize: 50),
            "storage_paths" => await StoragePathTools.List(_factory.Client, page: 2, pageSize: 50),
            "custom_fields" => await CustomFieldTools.List(_factory.Client, page: 2, pageSize: 50),
            _ => throw new ArgumentOutOfRangeException(nameof(tool), tool, null)
        };

        requestedPathAndQuery.Should().Be($"{expectedPath}?page=2&page_size=10");
        using var json = JsonDocument.Parse(result);
        json.RootElement.GetProperty("meta").GetProperty("page_size").GetInt32().Should().Be(10);
    }

    [Fact]
    public async Task PaperlessClient_DirectPaginationRequest_RespectsConfiguredMaxPageSize()
    {
        _factory.Options.MaxPageSize = 10;
        string? requestedPathAndQuery = null;

        _factory.MockHandler
            .When(HttpMethod.Get, "https://paperless.example.com/api/tags/*")
            .With(request =>
            {
                requestedPathAndQuery = request.RequestUri?.PathAndQuery;
                return true;
            })
            .Respond("application/json", EmptyPage);

        await _factory.Client.GetTagsAsync(page: 3, pageSize: 50);

        requestedPathAndQuery.Should().Be("/api/tags/?page=3&page_size=10");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void GetEffectivePageSize_WithInvalidConfiguredMaximum_UsesDefault(int configuredMaximum)
    {
        _factory.Options.MaxPageSize = configuredMaximum;

        _factory.Client.GetEffectivePageSize(250).Should().Be(100);
    }

    [Fact]
    public void GetEffectivePageSize_AllowsConfiguredMaximumAboveDefault()
    {
        _factory.Options.MaxPageSize = 250;

        _factory.Client.GetEffectivePageSize(200).Should().Be(200);
        _factory.Client.GetEffectivePageSize(300).Should().Be(250);
    }

    [Fact]
    public void GetEffectivePageSize_ClampsNonPositiveRequestToOne()
    {
        _factory.Options.MaxPageSize = 10;

        _factory.Client.GetEffectivePageSize(0).Should().Be(1);
    }
}
