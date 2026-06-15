using System.Text.Json;
using FluentAssertions;
using PaperlessMCP.Models.Common;
using PaperlessMCP.Models.Tags;
using PaperlessMCP.Tests.Fixtures;
using RichardSzalay.MockHttp;
using Xunit;

namespace PaperlessMCP.Tests.Client;

/// <summary>
/// Tests for the name→id resolvers used by the by-name metadata tools. They page the full
/// list and return every exact, case-insensitive match so callers can distinguish unknown (0),
/// resolved (1), and ambiguous (&gt;1).
/// </summary>
public class PaperlessClientFindByNameTests : IDisposable
{
    private readonly MockHttpClientFactory _factory;

    public PaperlessClientFindByNameTests()
    {
        _factory = new MockHttpClientFactory();
    }

    public void Dispose()
    {
        _factory.Dispose();
    }

    [Fact]
    public async Task FindTagsByNameAsync_WhenMatchExists_ReturnsSingleMatch()
    {
        _factory.SetupGet("api/tags/", TestFixtures.Tags.CreateTagListJson(3));

        var matches = await _factory.Client.FindTagsByNameAsync("Tag 2");

        matches.Should().ContainSingle();
        matches[0].Id.Should().Be(2);
    }

    [Fact]
    public async Task FindTagsByNameAsync_IsCaseInsensitive()
    {
        _factory.SetupGet("api/tags/", TestFixtures.Tags.CreateTagListJson(3));

        var matches = await _factory.Client.FindTagsByNameAsync("tAg 2");

        matches.Should().ContainSingle();
        matches[0].Id.Should().Be(2);
    }

    [Fact]
    public async Task FindTagsByNameAsync_WhenNoMatch_ReturnsEmpty()
    {
        _factory.SetupGet("api/tags/", TestFixtures.Tags.CreateTagListJson(3));

        var matches = await _factory.Client.FindTagsByNameAsync("Missing");

        matches.Should().BeEmpty();
    }

    [Fact]
    public async Task FindTagsByNameAsync_PagesThroughAllResults()
    {
        // The match only appears on page 2, so the resolver must follow `next`.
        var page1 = JsonSerializer.Serialize(new PaginatedResult<Tag>
        {
            Count = 3,
            Next = "https://paperless.example.com/api/tags/?page=2",
            Results = [TestFixtures.Tags.CreateTag(1, "Tag 1"), TestFixtures.Tags.CreateTag(2, "Tag 2")]
        });
        var page2 = JsonSerializer.Serialize(new PaginatedResult<Tag>
        {
            Count = 3,
            Next = null,
            Results = [TestFixtures.Tags.CreateTag(99, "Target")]
        });

        _factory.MockHandler
            .When(HttpMethod.Get, $"{_factory.Options.BaseUrl}/api/tags/")
            .WithQueryString("page", "1")
            .Respond("application/json", page1);
        _factory.MockHandler
            .When(HttpMethod.Get, $"{_factory.Options.BaseUrl}/api/tags/")
            .WithQueryString("page", "2")
            .Respond("application/json", page2);

        var matches = await _factory.Client.FindTagsByNameAsync("Target");

        matches.Should().ContainSingle();
        matches[0].Id.Should().Be(99);
    }

    [Fact]
    public async Task FindDocumentTypesByNameAsync_WhenMatchExists_ReturnsSingleMatch()
    {
        _factory.SetupGet("api/document_types/", TestFixtures.DocumentTypes.CreateDocumentTypeListJson(3));

        var matches = await _factory.Client.FindDocumentTypesByNameAsync("Type 3");

        matches.Should().ContainSingle();
        matches[0].Id.Should().Be(3);
    }

    [Fact]
    public async Task FindCorrespondentsByNameAsync_WhenMatchExists_ReturnsSingleMatch()
    {
        _factory.SetupGet("api/correspondents/", TestFixtures.Correspondents.CreateCorrespondentListJson(3));

        var matches = await _factory.Client.FindCorrespondentsByNameAsync("Correspondent 1");

        matches.Should().ContainSingle();
        matches[0].Id.Should().Be(1);
    }

    [Fact]
    public async Task FindCorrespondentsByNameAsync_WhenNoMatch_ReturnsEmpty()
    {
        _factory.SetupGet("api/correspondents/", TestFixtures.Correspondents.CreateCorrespondentListJson(3));

        var matches = await _factory.Client.FindCorrespondentsByNameAsync("Nobody");

        matches.Should().BeEmpty();
    }
}
