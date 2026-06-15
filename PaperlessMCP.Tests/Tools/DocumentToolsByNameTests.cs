using System.Net;
using System.Text.Json;
using FluentAssertions;
using PaperlessMCP.Models.Common;
using PaperlessMCP.Models.Documents;
using PaperlessMCP.Models.Tags;
using PaperlessMCP.Tests.Fixtures;
using PaperlessMCP.Tools;
using RichardSzalay.MockHttp;
using Xunit;

namespace PaperlessMCP.Tests.Tools;

/// <summary>
/// Tests for the name-based metadata tools: add/remove tags by name and the
/// documentTypeName / correspondentName parameters on update. These let an LLM set
/// metadata by NAME without ever transcribing numeric IDs; resolution is lookup-only,
/// atomic, and case-insensitive, and any unknown/ambiguous name must fail loudly without
/// touching the document.
/// </summary>
public class DocumentToolsByNameTests : IDisposable
{
    private readonly MockHttpClientFactory _factory;

    public DocumentToolsByNameTests()
    {
        _factory = new MockHttpClientFactory();
    }

    public void Dispose()
    {
        _factory.Dispose();
    }

    private string PatchUrl(int id) => $"{_factory.Options.BaseUrl}/api/documents/{id}/";

    private static string DocJsonWithTags(int id, params int[] tags) =>
        JsonSerializer.Serialize(TestFixtures.Documents.CreateDocument(id) with { Tags = tags.ToList() });

    private static string TagListJson(params (int id, string name)[] tags) =>
        JsonSerializer.Serialize(new PaginatedResult<Tag>
        {
            Count = tags.Length,
            Results = tags.Select(t => TestFixtures.Tags.CreateTag(t.id, t.name)).ToList()
        });

    #region Add tags by name

    [Fact]
    public async Task AddTagsByName_ResolvesNameAndUnionsWithExistingTags()
    {
        // Doc has [1,2] ("Tag 1","Tag 2"); adding "Tag 3" must PATCH the union [1,2,3].
        _factory.SetupGet("api/tags/", TestFixtures.Tags.CreateTagListJson(3));
        _factory.SetupGet("api/documents/1/", DocJsonWithTags(1, 1, 2));
        var patch = _factory.MockHandler
            .When(HttpMethod.Patch, PatchUrl(1))
            .WithPartialContent("\"tags\":[1,2,3]")
            .Respond("application/json", DocJsonWithTags(1, 1, 2, 3));

        var result = await DocumentTools.AddTagsByName(_factory.Client, 1, "Tag 3");

        var json = JsonDocument.Parse(result);
        json.RootElement.GetProperty("ok").GetBoolean().Should().BeTrue();
        _factory.MockHandler.GetMatchCount(patch).Should().Be(1);
        json.RootElement.GetProperty("result").GetProperty("tags").GetArrayLength().Should().Be(3);
    }

    [Fact]
    public async Task AddTagsByName_IsCaseInsensitive()
    {
        _factory.SetupGet("api/tags/", TestFixtures.Tags.CreateTagListJson(3));
        _factory.SetupGet("api/documents/1/", DocJsonWithTags(1, 1, 2));
        var patch = _factory.MockHandler
            .When(HttpMethod.Patch, PatchUrl(1))
            .WithPartialContent("\"tags\":[1,2,3]")
            .Respond("application/json", DocJsonWithTags(1, 1, 2, 3));

        var result = await DocumentTools.AddTagsByName(_factory.Client, 1, "tag 3");

        JsonDocument.Parse(result).RootElement.GetProperty("ok").GetBoolean().Should().BeTrue();
        _factory.MockHandler.GetMatchCount(patch).Should().Be(1);
    }

    [Fact]
    public async Task AddTagsByName_ResolvesMultipleNamesInOneCall()
    {
        // Adding "Tag 3" and the already-present "Tag 1" yields the union [1,2,3].
        _factory.SetupGet("api/tags/", TestFixtures.Tags.CreateTagListJson(3));
        _factory.SetupGet("api/documents/1/", DocJsonWithTags(1, 1, 2));
        var patch = _factory.MockHandler
            .When(HttpMethod.Patch, PatchUrl(1))
            .WithPartialContent("\"tags\":[1,2,3]")
            .Respond("application/json", DocJsonWithTags(1, 1, 2, 3));

        var result = await DocumentTools.AddTagsByName(_factory.Client, 1, "Tag 3, Tag 1");

        JsonDocument.Parse(result).RootElement.GetProperty("ok").GetBoolean().Should().BeTrue();
        _factory.MockHandler.GetMatchCount(patch).Should().Be(1);
    }

    [Fact]
    public async Task AddTagsByName_WhenTagAlreadyPresent_IsNoOpAndDoesNotPatch()
    {
        _factory.SetupGet("api/tags/", TestFixtures.Tags.CreateTagListJson(3));
        _factory.SetupGet("api/documents/1/", DocJsonWithTags(1, 1, 2));
        var patch = _factory.SetupPatch("api/documents/1/", DocJsonWithTags(1, 1, 2));

        var result = await DocumentTools.AddTagsByName(_factory.Client, 1, "Tag 1");

        JsonDocument.Parse(result).RootElement.GetProperty("ok").GetBoolean().Should().BeTrue();
        _factory.MockHandler.GetMatchCount(patch).Should().Be(0);
    }

    [Fact]
    public async Task AddTagsByName_WithUnknownName_ReturnsValidationErrorAndDoesNotPatch()
    {
        _factory.SetupGet("api/tags/", TestFixtures.Tags.CreateTagListJson(3));
        var patch = _factory.SetupPatch("api/documents/1/", DocJsonWithTags(1, 1, 2));

        var result = await DocumentTools.AddTagsByName(_factory.Client, 1, "Nonexistent");

        var json = JsonDocument.Parse(result);
        json.RootElement.GetProperty("ok").GetBoolean().Should().BeFalse();
        json.RootElement.GetProperty("error").GetProperty("code").GetString().Should().Be("VALIDATION");
        json.RootElement.GetProperty("error").GetProperty("details").GetProperty("unknown_tags")
            .EnumerateArray().Select(e => e.GetString()).Should().Contain("Nonexistent");
        _factory.MockHandler.GetMatchCount(patch).Should().Be(0);
    }

    [Fact]
    public async Task AddTagsByName_WithAmbiguousName_ReturnsValidationErrorAndDoesNotPatch()
    {
        // Two tags share the name "Dup" — must refuse rather than pick one.
        _factory.SetupGet("api/tags/", TagListJson((5, "Dup"), (6, "Dup")));
        var patch = _factory.SetupPatch("api/documents/1/", DocJsonWithTags(1, 1, 2));

        var result = await DocumentTools.AddTagsByName(_factory.Client, 1, "Dup");

        var json = JsonDocument.Parse(result);
        json.RootElement.GetProperty("ok").GetBoolean().Should().BeFalse();
        json.RootElement.GetProperty("error").GetProperty("code").GetString().Should().Be("VALIDATION");
        json.RootElement.GetProperty("error").GetProperty("details").GetProperty("ambiguous_tags")
            .EnumerateArray().Select(e => e.GetString()).Should().Contain("Dup");
        _factory.MockHandler.GetMatchCount(patch).Should().Be(0);
    }

    [Fact]
    public async Task AddTagsByName_WithEmptyNames_ReturnsValidationError()
    {
        var result = await DocumentTools.AddTagsByName(_factory.Client, 1, "   ");

        var json = JsonDocument.Parse(result);
        json.RootElement.GetProperty("ok").GetBoolean().Should().BeFalse();
        json.RootElement.GetProperty("error").GetProperty("code").GetString().Should().Be("VALIDATION");
    }

    [Fact]
    public async Task AddTagsByName_WhenDocumentNotFound_ReturnsNotFound()
    {
        _factory.SetupGet("api/tags/", TestFixtures.Tags.CreateTagListJson(3));
        _factory.SetupGetWithStatus("api/documents/999/", HttpStatusCode.NotFound);

        var result = await DocumentTools.AddTagsByName(_factory.Client, 999, "Tag 1");

        var json = JsonDocument.Parse(result);
        json.RootElement.GetProperty("ok").GetBoolean().Should().BeFalse();
        json.RootElement.GetProperty("error").GetProperty("code").GetString().Should().Be("NOT_FOUND");
    }

    #endregion

    #region Remove tags by name

    [Fact]
    public async Task RemoveTagsByName_SubtractsResolvedTag_NeedsTriageScenario()
    {
        // Doc tagged [6,9] = ["2025","needstriage"]; removing "needstriage" leaves [6].
        _factory.SetupGet("api/tags/", TagListJson((6, "2025"), (9, "needstriage"), (2, "Tag 2")));
        _factory.SetupGet("api/documents/1/", DocJsonWithTags(1, 6, 9));
        var patch = _factory.MockHandler
            .When(HttpMethod.Patch, PatchUrl(1))
            .WithPartialContent("\"tags\":[6]")
            .Respond("application/json", DocJsonWithTags(1, 6));

        var result = await DocumentTools.RemoveTagsByName(_factory.Client, 1, "needstriage");

        var json = JsonDocument.Parse(result);
        json.RootElement.GetProperty("ok").GetBoolean().Should().BeTrue();
        _factory.MockHandler.GetMatchCount(patch).Should().Be(1);
        var tags = json.RootElement.GetProperty("result").GetProperty("tags");
        tags.GetArrayLength().Should().Be(1);
        tags[0].GetProperty("id").GetInt32().Should().Be(6);
        tags[0].GetProperty("name").GetString().Should().Be("2025");
    }

    [Fact]
    public async Task RemoveTagsByName_WhenTagAbsent_IsNoOpAndDoesNotPatch()
    {
        _factory.SetupGet("api/tags/", TestFixtures.Tags.CreateTagListJson(3));
        _factory.SetupGet("api/documents/1/", DocJsonWithTags(1, 1, 2));
        var patch = _factory.SetupPatch("api/documents/1/", DocJsonWithTags(1, 1, 2));

        var result = await DocumentTools.RemoveTagsByName(_factory.Client, 1, "Tag 3");

        JsonDocument.Parse(result).RootElement.GetProperty("ok").GetBoolean().Should().BeTrue();
        _factory.MockHandler.GetMatchCount(patch).Should().Be(0);
    }

    #endregion

    #region Update by name (document type / correspondent)

    [Fact]
    public async Task Update_WithDocumentTypeName_ResolvesAndSetsId()
    {
        _factory.SetupGet("api/document_types/", TestFixtures.DocumentTypes.CreateDocumentTypeListJson(3));
        var patch = _factory.MockHandler
            .When(HttpMethod.Patch, PatchUrl(1))
            .WithPartialContent("\"document_type\":2")
            .Respond("application/json", TestFixtures.Documents.CreateDocumentJson(1, "X"));

        var result = await DocumentTools.Update(_factory.Client, 1, documentTypeName: "Type 2");

        JsonDocument.Parse(result).RootElement.GetProperty("ok").GetBoolean().Should().BeTrue();
        _factory.MockHandler.GetMatchCount(patch).Should().Be(1);
    }

    [Fact]
    public async Task Update_WithCorrespondentName_ResolvesAndSetsId()
    {
        _factory.SetupGet("api/correspondents/", TestFixtures.Correspondents.CreateCorrespondentListJson(3));
        var patch = _factory.MockHandler
            .When(HttpMethod.Patch, PatchUrl(1))
            .WithPartialContent("\"correspondent\":2")
            .Respond("application/json", TestFixtures.Documents.CreateDocumentJson(1, "X"));

        var result = await DocumentTools.Update(_factory.Client, 1, correspondentName: "Correspondent 2");

        JsonDocument.Parse(result).RootElement.GetProperty("ok").GetBoolean().Should().BeTrue();
        _factory.MockHandler.GetMatchCount(patch).Should().Be(1);
    }

    [Fact]
    public async Task Update_WithUnknownDocumentTypeName_ReturnsValidationErrorAndDoesNotPatch()
    {
        _factory.SetupGet("api/document_types/", TestFixtures.DocumentTypes.CreateDocumentTypeListJson(3));
        var patch = _factory.SetupPatch("api/documents/1/", TestFixtures.Documents.CreateDocumentJson(1));

        var result = await DocumentTools.Update(_factory.Client, 1, documentTypeName: "Nope");

        var json = JsonDocument.Parse(result);
        json.RootElement.GetProperty("ok").GetBoolean().Should().BeFalse();
        json.RootElement.GetProperty("error").GetProperty("code").GetString().Should().Be("VALIDATION");
        _factory.MockHandler.GetMatchCount(patch).Should().Be(0);
    }

    #endregion
}
