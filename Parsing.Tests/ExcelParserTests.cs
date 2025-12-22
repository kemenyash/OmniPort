using OmniPort.Core.Interfaces;
using OmniPort.Core.Parsers;

namespace Parsing.Tests;

public class ExcelParserTests
{
    [Fact]
    public void Should_Parse_Excel_File()
    {
        IImportParser parser = new ExcelImportParser();
        string path = Path.Combine(AppContext.BaseDirectory, "TestData", "sample.xlsx");
        Assert.True(File.Exists(path), $"Файл не знайдено: {path}");

        using FileStream stream = File.OpenRead(path);

        List<IDictionary<string, object?>> records = parser.Parse(stream).ToList();

        Assert.NotEmpty(records);

        string? actualName = records[0]["Name"]?.ToString()?.Trim();
        string? actualAge = records[0]["Age"]?.ToString()?.Trim();

        Assert.Equal("Vlad", actualName);
        Assert.Equal("35", actualAge);
    }
}
