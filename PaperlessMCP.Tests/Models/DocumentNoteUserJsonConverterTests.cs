using System.Text.Json;
using FluentAssertions;
using PaperlessMCP.Models.Documents;
using Xunit;

namespace PaperlessMCP.Tests.Models;

public class DocumentNoteUserJsonConverterTests
{
    [Fact]
    public void Deserialize_ObjectUser_ReadsPaperlessV8AndLaterShape()
    {
        const string json = """
            {
              "id": 9,
              "note": "Reviewed",
              "created": "2026-06-02T11:00:00Z",
              "user": {
                "id": 2,
                "username": "alice",
                "first_name": "Alice",
                "last_name": "Doe"
              }
            }
            """;

        var note = JsonSerializer.Deserialize<DocumentNote>(json);

        note.Should().NotBeNull();
        note!.User.Should().NotBeNull();
        note.User!.Id.Should().Be(2);
        note.User.Username.Should().Be("alice");
        note.User.FirstName.Should().Be("Alice");
        note.User.LastName.Should().Be("Doe");
    }

    [Fact]
    public void RoundTrip_NumericUser_PreservesLegacyShape()
    {
        const string json = """
            {
              "id": 9,
              "note": "Reviewed",
              "created": "2026-06-02T11:00:00Z",
              "user": 2
            }
            """;

        var note = JsonSerializer.Deserialize<DocumentNote>(json);
        var serialized = JsonSerializer.Serialize(note);

        note.Should().NotBeNull();
        note!.User.Should().NotBeNull();
        note.User!.Id.Should().Be(2);
        serialized.Should().Contain("\"user\":2");
    }
}
