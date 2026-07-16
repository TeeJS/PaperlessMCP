using FluentAssertions;
using PaperlessMCP.Utils;
using Xunit;

namespace PaperlessMCP.Tests.Utils;

public class ParsingHelpersTests
{
    [Theory]
    [InlineData("60", 60)]
    [InlineData("300", 300)]
    [InlineData("1", 1)]
    public void ParsePositiveInt_WithValidValue_ReturnsParsedValue(string input, int expected)
    {
        ParsingHelpers.ParsePositiveInt(input, fallback: 30).Should().Be(expected);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not-a-number")]
    [InlineData("0")]
    [InlineData("-5")]
    public void ParsePositiveInt_WithInvalidOrNonPositiveValue_ReturnsFallback(string? input)
    {
        ParsingHelpers.ParsePositiveInt(input, fallback: 30).Should().Be(30);
    }
}
