using OmniPort.Core.Interfaces;
using OmniPort.Core.Parsers;

namespace Parsing.Tests;

public class JsonParserTests
{
    [Fact]
    public void Should_Parse_Json_File()
    {
        IImportParser parser = new JsonImportParser();
        string path = Path.Combine(AppContext.BaseDirectory, "TestData", "sample.json");
        Assert.True(File.Exists(path), $"Файл не знайдено: {path}");

        using FileStream stream = File.OpenRead(path);

        List<IDictionary<string, object?>> records = parser.Parse(stream).ToList();

        Assert.NotEmpty(records);
        Assert.Contains("Name", records[0].Keys);
        Assert.Contains("Email", records[0].Keys);
    }
}
