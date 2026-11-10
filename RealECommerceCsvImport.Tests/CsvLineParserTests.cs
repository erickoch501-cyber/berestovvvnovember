using ECommerceCsvImport.Services;
using Xunit;

namespace ECommerceCsvImport.Tests;

/// <summary>
/// Тесты разбора строки CSV.
/// </summary>
public class CsvLineParserTests
{
    [Fact]
    public void Parse_SimpleLine_ReturnsAllFields()
    {
        string line = "1001,Ноутбук Lenovo IdeaPad 3,Бюджетный ноутбук,45990,RUB,Компьютеры,5,/images/laptop.jpg,true,Lenovo,24,IdeaPad3-15";

        var fields = CsvLineParser.Parse(line);

        Assert.Equal(12, fields.Count);
        Assert.Equal("1001", fields[0]);
    }

    [Fact]
    public void Parse_EmptyFields_AreReturnedAsEmptyStrings()
    {
        string line = "1011,Блок питания Corsair CV550,,4990,RUB,Компьютеры,9,,true,Corsair,60,CV550";

        var fields = CsvLineParser.Parse(line);

        Assert.Equal(12, fields.Count);
        Assert.Equal(string.Empty, fields[2]);
        Assert.Equal(string.Empty, fields[7]);
    }

    [Fact]
    public void Parse_CommaInsideQuotes_DoesNotSplitField()
    {
        string line = "1012,\"Ноутбук, игровой\",Тестовая строка,59990,RUB,Компьютеры,1,/images/notebook.jpg,true,ASUS,36,VivoBook";

        var fields = CsvLineParser.Parse(line);

        Assert.Equal(12, fields.Count);
        Assert.Equal("Ноутбук, игровой", fields[1]);
    }

    [Fact]
    public void Parse_EscapedQuotes_AreUnescaped()
    {
        string line = "1013,\"Название \"\"Special\"\"\",Описание,100,RUB,Категория,5,/img.jpg,true,Brand,12,Model";

        var fields = CsvLineParser.Parse(line);

        Assert.Equal("Название \"Special\"", fields[1]);
    }
}
