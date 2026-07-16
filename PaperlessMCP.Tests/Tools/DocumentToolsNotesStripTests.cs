using System.Text.Json;
using FluentAssertions;
using PaperlessMCP.Models.Documents;
using PaperlessMCP.Tests.Fixtures;
using PaperlessMCP.Tools;
using Xunit;

namespace PaperlessMCP.Tests.Tools;

/// <summary>
/// A document's notes can be hundreds of thousands of characters (an AI-OCR pass duplicates
/// the body as markdown), which overflows the LLM tool-result limit and makes a successful
/// response look like a failure. Every tool that returns a full <see cref="Document"/> must
/// omit notes by default and offer includeNotes as an opt-in. documents_get already does this
/// (covered here as regression); these tests also pin the same behavior for documents_update.
/// </summary>
public class DocumentToolsNotesStripTests : IDisposable
{
    private readonly MockHttpClientFactory _factory;
    private const string NoteMarker = "NOTE-BODY-THAT-SHOULD-BE-STRIPPED";

    public DocumentToolsNotesStripTests() => _factory = new MockHttpClientFactory();

    public void Dispose() => _factory.Dispose();

    private static string DocJsonWithNotes(int id, string content = "OCR content") =>
        JsonSerializer.Serialize(TestFixtures.Documents.CreateDocument(id) with
        {
            Content = content,
            Notes = new List<DocumentNote>
            {
                new() { Id = 1, Note = NoteMarker, Created = DateTime.UtcNow, User = new DocumentNoteUser { Id = 7 } }
            }
        });

    [Fact]
    public async Task Get_ByDefault_OmitsNotes()
    {
        _factory.SetupGet("api/documents/5549/", DocJsonWithNotes(5549));

        var result = await DocumentTools.Get(_factory.Client, 5549);

        result.Should().NotContain(NoteMarker);
        JsonDocument.Parse(result).RootElement.GetProperty("result").GetProperty("notes")
            .ValueKind.Should().Be(JsonValueKind.Null);
    }

    [Fact]
    public async Task Get_WithIncludeNotes_ReturnsNotes()
    {
        _factory.SetupGet("api/documents/5549/", DocJsonWithNotes(5549));

        var result = await DocumentTools.Get(_factory.Client, 5549, includeNotes: true);

        result.Should().Contain(NoteMarker);
        var notes = JsonDocument.Parse(result).RootElement.GetProperty("result").GetProperty("notes");
        notes.ValueKind.Should().Be(JsonValueKind.Array);
        notes.GetArrayLength().Should().Be(1);
    }

    [Fact]
    public async Task Get_WithContentMaxLength_TruncatesContent()
    {
        _factory.SetupGet("api/documents/5549/", DocJsonWithNotes(5549, content: new string('a', 5000)));

        var result = await DocumentTools.Get(_factory.Client, 5549, contentMaxLength: 100);

        var content = JsonDocument.Parse(result).RootElement.GetProperty("result")
            .GetProperty("content").GetString();
        content.Should().HaveLength(103); // 100 chars + "..."
        content.Should().EndWith("...");
    }

    [Fact]
    public async Task Update_ByDefault_OmitsNotesFromReturnedDocument()
    {
        // The PATCH response echoes the document WITH its (huge) notes; the tool must strip them
        // so the success response stays small. This is the live "update looks like a failure" bug.
        _factory.SetupPatch("api/documents/5549/", DocJsonWithNotes(5549));

        var result = await DocumentTools.Update(_factory.Client, 5549, title: "New Title");

        result.Should().NotContain(NoteMarker);
        var json = JsonDocument.Parse(result);
        json.RootElement.GetProperty("ok").GetBoolean().Should().BeTrue();
        json.RootElement.GetProperty("result").GetProperty("notes")
            .ValueKind.Should().Be(JsonValueKind.Null);
    }

    [Fact]
    public async Task Update_WithIncludeNotes_ReturnsNotes()
    {
        _factory.SetupPatch("api/documents/5549/", DocJsonWithNotes(5549));

        var result = await DocumentTools.Update(_factory.Client, 5549, title: "New Title", includeNotes: true);

        result.Should().Contain(NoteMarker);
        JsonDocument.Parse(result).RootElement.GetProperty("result").GetProperty("notes")
            .ValueKind.Should().Be(JsonValueKind.Array);
    }
}
