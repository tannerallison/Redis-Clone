using FluentAssertions;

namespace InMemCache.Tests;

public class RedisTests
{
    [Fact]
    public void DeserializeString()
    {
        var resp = new Redis().Deserialize("+A valid string\r\n");

        var respString = resp as RESPString;
        respString.Should().NotBeNull();
        respString?.Value.Should().Be("A valid string");
    }

    [Fact]
    public void DeserializeInteger()
    {
        var resp = new Redis().Deserialize(":83298\r\n");

        var respString = resp as RESPInteger;
        respString.Should().NotBeNull();
        respString?.Value.Should().Be(83298);
    }

    [Fact]
    public void DeserializeBulkStrings()
    {
        const string val = "This is a value";
        var resp = new Redis().Deserialize($"${System.Text.ASCIIEncoding.ASCII.GetByteCount(val)}\r\n{val}r\n");
        var respString = resp as RESPBulkString;
        respString.Should().NotBeNull();
        respString?.Value.Should().Be(val);
    }

    [Fact]
    public void DeserializeMultilineBulkStrings()
    {
        const string multiline = "This value\r\nHas multiple lines";
        var resp = new Redis().Deserialize(
            $"${System.Text.ASCIIEncoding.ASCII.GetByteCount(multiline)}\r\n{multiline}r\n");

        var respString = resp as RESPBulkString;
        respString.Should().NotBeNull();
        respString?.Value.Should().Be(multiline);
    }

    [Fact]
    public void DeserializeEmptyBulkString()
    {
        var resp = new Redis().Deserialize($"$0\r\n\r\n");
        var respString = resp as RESPBulkString;
        respString.Should().NotBeNull();
        respString?.Value.Should().Be("");
    }

    [Fact]
    public void DeserializeArrays()
    {
        var resp = new Redis().Deserialize("*5\r\n:1\r\n:2\r\n:3\r\n:4\r\n$5\r\nhello\r\n");

        var respString = resp as RESPArray;
        respString.Should().NotBeNull();
        respString?.Value.Should().HaveCount(5);
        respString?.Value[4].Should().BeOfType<RESPBulkString>();
    }

    [Fact]
    public void DeserializeArraysWithinArrays()
    {
        var resp = new Redis().Deserialize("*5\r\n:1\r\n*3\r\n:11\r\nYes\r\n+SomeValue\r\n:3\r\n:4\r\n$5\r\nhello\r\n");

        var respString = resp as RESPArray;
        respString.Should().NotBeNull();
        respString?.Value.Should().HaveCount(5);
        respString?.Value[1].Should().BeOfType<RESPArray>();
        (respString?.Value[1] as RESPArray).Value[1].Should().BeNull();
    }
}
