using ECommerceCsvImport.Models;
using ECommerceCsvImport.Services;
using Xunit;

namespace ECommerceCsvImport.Tests;

/// <summary>
/// Тесты ProductImportService (вариант 1 "Электроника").
/// </summary>
public class ProductImportServiceTests
{
    private const string Header =
        "sku,name,description,price,currency,category_name,stock,image_url,is_active,brand,warranty_months,model_number";

    private const string LaptopRow =
        "1001,Ноутбук Lenovo IdeaPad 3,Бюджетный ноутбук для работы и учебы,45990,RUB,Компьютеры,5,/images/laptop.jpg,true,Lenovo,24,IdeaPad3-15";

    private const string MouseRow =
        "1002,Мышь Logitech M185,Беспроводная мышь с USB-приемником,1290,RUB,Периферия,10,/images/mouse.jpg,true,Logitech,12,M185";

    private const string AsusRow =
        "1001,Ноутбук ASUS VivoBook,Другой ноутбук,52990,RUB,Компьютеры,7,/images/asus.jpg,true,ASUS,24,VivoBook";

    private const string CorsairRow =
        "1011,Блок питания Corsair CV550,,4990,RUB,Компьютеры,9,,true,Corsair,60,CV550";

    private static (InMemoryProductRepository Repository, ProductImportService Service) CreateService()
    {
        var repository = new InMemoryProductRepository();
        return (repository, new ProductImportService(repository));
    }

    /// <summary>Проверяет, что строка отклонена и среди ошибок есть нужное сообщение.</summary>
    private static void AssertRejected(ImportResult result, string expectedMessage)
    {
        Assert.Equal(0, result.ImportedCount);
        Assert.NotEmpty(result.Errors);
        Assert.Contains(result.Errors, error => error.Message.Contains(expectedMessage));
    }

    private static string CreateTempPath()
    {
        return Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".csv");
    }

    [Fact]
    public void ImportFromLines_OneValidRow_ImportsProduct()
    {
        var (repository, service) = CreateService();
        var lines = new[] { Header, LaptopRow };

        var result = service.ImportFromLines(lines);

        Assert.Equal(1, result.TotalRows);
        Assert.Equal(1, result.ImportedCount);
        Assert.Empty(result.Errors);

        var product = repository.GetBySku("1001");
        Assert.NotNull(product);
        Assert.Equal("Ноутбук Lenovo IdeaPad 3", product!.Name);
        Assert.Equal(45990m, product.Price);
        Assert.Equal("Lenovo", product.Brand);
        Assert.Equal(24, product.WarrantyMonths);
        Assert.Equal("IdeaPad3-15", product.ModelNumber);
    }

    [Fact]
    public void ImportFromLines_SeveralValidRows_ImportsAll()
    {
        var (repository, service) = CreateService();
        var lines = new[] { Header, LaptopRow, MouseRow };

        var result = service.ImportFromLines(lines);

        Assert.Equal(2, result.TotalRows);
        Assert.Equal(2, result.ImportedCount);
        Assert.Empty(result.Errors);
        Assert.Equal(2, repository.Count);
    }

    [Fact]
    public void ImportFromCsv_FileNotFound_ReturnsError()
    {
        var (_, service) = CreateService();
        var path = CreateTempPath();

        var result = service.ImportFromCsv(path);

        Assert.Equal(0, result.TotalRows);
        Assert.Equal(0, result.ImportedCount);
        Assert.Single(result.Errors);
        Assert.Contains("Файл не найден", result.Errors[0].Message);
    }

    [Fact]
    public void ImportFromCsv_EmptyFile_ReturnsNoDataError()
    {
        var (_, service) = CreateService();
        var path = CreateTempPath();
        File.WriteAllText(path, string.Empty);

        try
        {
            var result = service.ImportFromCsv(path);

            Assert.Equal(0, result.TotalRows);
            Assert.Equal(0, result.ImportedCount);
            Assert.Single(result.Errors);
            Assert.Contains("Данные для импорта отсутствуют", result.Errors[0].Message);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void ImportFromLines_HeaderOnly_ReturnsNoDataError()
    {
        var (_, service) = CreateService();
        var lines = new[] { Header };

        var result = service.ImportFromLines(lines);

        Assert.Equal(0, result.TotalRows);
        Assert.Equal(0, result.ImportedCount);
        Assert.Single(result.Errors);
        Assert.Contains("Данные для импорта отсутствуют", result.Errors[0].Message);
    }

    [Fact]
    public void ImportFromLines_WrongHeader_ReturnsHeaderError()
    {
        var (_, service) = CreateService();
        var lines = new[]
        {
            "sku,name,description,price,currency,category_name,stock,image_url,is_active"
        };

        var result = service.ImportFromLines(lines);

        Assert.Equal(0, result.TotalRows);
        Assert.Equal(0, result.ImportedCount);
        Assert.NotEmpty(result.Errors);
        Assert.Contains("Неверные заголовки CSV", result.Errors[0].Message);
    }

    [Fact]
    public void ImportFromLines_WrongFieldCount_ReturnsError()
    {
        var (_, service) = CreateService();
        var lines = new[] { Header, "1001,Ноутбук Lenovo IdeaPad 3" };

        var result = service.ImportFromLines(lines);

        Assert.Equal(1, result.TotalRows);
        AssertRejected(result, "Неверное количество полей в строке");
    }

    [Fact]
    public void ImportFromLines_EmptySku_ReturnsError()
    {
        var (_, service) = CreateService();
        var lines = new[]
        {
            Header,
            ",Ноутбук Lenovo IdeaPad 3,Бюджетный ноутбук,45990,RUB,Компьютеры,5,/images/laptop.jpg,true,Lenovo,24,IdeaPad3-15"
        };

        var result = service.ImportFromLines(lines);

        AssertRejected(result, "Поле sku не может быть пустым");
    }

    [Fact]
    public void ImportFromLines_EmptyName_ReturnsError()
    {
        var (_, service) = CreateService();
        var lines = new[]
        {
            Header,
            "1001,,Бюджетный ноутбук,45990,RUB,Компьютеры,5,/images/laptop.jpg,true,Lenovo,24,IdeaPad3-15"
        };

        var result = service.ImportFromLines(lines);

        AssertRejected(result, "Поле name не может быть пустым");
    }

    [Fact]
    public void ImportFromLines_NegativePrice_ReturnsError()
    {
        var (_, service) = CreateService();
        var lines = new[]
        {
            Header,
            "1003,Клавиатура A4Tech Bloody V3M,Механическая игровая клавиатура,-500,RUB,Периферия,7,/images/keyboard.jpg,true,A4Tech,24,V3M"
        };

        var result = service.ImportFromLines(lines);

        AssertRejected(result, "Поле price должно быть числом больше 0");
    }

    [Fact]
    public void ImportFromLines_NonNumericPrice_ReturnsError()
    {
        var (_, service) = CreateService();
        var lines = new[]
        {
            Header,
            "1003,Клавиатура A4Tech Bloody V3M,Механическая игровая клавиатура,abc,RUB,Периферия,7,/images/keyboard.jpg,true,A4Tech,24,V3M"
        };

        var result = service.ImportFromLines(lines);

        AssertRejected(result, "Поле price должно быть числом больше 0");
    }

    [Fact]
    public void ImportFromLines_NegativeStock_ReturnsError()
    {
        var (_, service) = CreateService();
        var lines = new[]
        {
            Header,
            "1004,Монитор Dell S2722QC,Игровой монитор 27 дюймов,29990,RUB,Компьютеры,-5,/images/monitor.jpg,true,Dell,24,S2722QC"
        };

        var result = service.ImportFromLines(lines);

        AssertRejected(result, "Остаток на складе должен быть целым числом не меньше 0");
    }

    [Fact]
    public void ImportFromLines_InvalidIsActive_ReturnsError()
    {
        var (_, service) = CreateService();
        var lines = new[]
        {
            Header,
            "1007,Webcam Logitech C920,Веб-камера Full HD,4990,RUB,Периферия,6,/images/webcam.jpg,yes,Logitech,24,C920"
        };

        var result = service.ImportFromLines(lines);

        AssertRejected(result, "Поле is_active должно иметь значение true, false, 1 или 0");
    }

    [Fact]
    public void ImportFromLines_MissingBrand_ReturnsError()
    {
        var (_, service) = CreateService();
        var lines = new[]
        {
            Header,
            "1005,Видеокарта NVIDIA GeForce RTX 3060,,24990,RUB,Компьютеры,4,/images/gpu.jpg,true,,36,RTX3060"
        };

        var result = service.ImportFromLines(lines);

        AssertRejected(result, "Поле brand обязательно для варианта «Электроника»");
    }

    [Fact]
    public void ImportFromLines_WarrantyBelowZero_ReturnsError()
    {
        var (_, service) = CreateService();
        var lines = new[]
        {
            Header,
            "1006,Накопитель SSD Samsung 870 EVO 1TB,Внутренний твердотельный накопитель,5990,RUB,Компьютеры,12,/images/ssd.jpg,true,Samsung,-1,870EVO-1T"
        };

        var result = service.ImportFromLines(lines);

        AssertRejected(result, "Гарантия должна быть целым числом от 0 до 60 месяцев");
    }

    [Fact]
    public void ImportFromLines_WarrantyAbove60_ReturnsError()
    {
        var (_, service) = CreateService();
        var lines = new[]
        {
            Header,
            "1006,Накопитель SSD Samsung 870 EVO 1TB,Внутренний твердотельный накопитель,5990,RUB,Компьютеры,12,/images/ssd.jpg,true,Samsung,61,870EVO-1T"
        };

        var result = service.ImportFromLines(lines);

        AssertRejected(result, "Гарантия должна быть целым числом от 0 до 60 месяцев");
    }

    [Fact]
    public void ImportFromLines_DuplicateSkuInsideFile_SkipsSecondRow()
    {
        var (repository, service) = CreateService();
        var lines = new[] { Header, LaptopRow, AsusRow };

        var result = service.ImportFromLines(lines);

        Assert.Equal(2, result.TotalRows);
        Assert.Equal(1, result.ImportedCount);
        Assert.Single(result.Errors);
        Assert.Contains("Артикул товара уже существует", result.Errors[0].Message);

        var product = repository.GetBySku("1001");
        Assert.NotNull(product);
        Assert.Equal("Ноутбук Lenovo IdeaPad 3", product!.Name);
    }

    [Fact]
    public void ImportFromLines_DuplicateSkuWithExistingProduct_SkipsRow()
    {
        var (repository, service) = CreateService();
        repository.Add(new Product { Sku = "1001", Name = "Уже существующий товар", Brand = "Lenovo" });
        var lines = new[] { Header, AsusRow };

        var result = service.ImportFromLines(lines);

        Assert.Equal(1, repository.Count);
        AssertRejected(result, "Артикул товара уже существует");
    }

    [Fact]
    public void ImportFromLines_SeveralErrorsInOneRow_ReturnsAllErrors()
    {
        var (repository, service) = CreateService();
        var lines = new[]
        {
            Header,
            ",,Некорректная строка,-10,RUB,Компьютеры,-5,/images/x.jpg,yes,,61,"
        };

        var result = service.ImportFromLines(lines);

        Assert.Equal(0, result.ImportedCount);
        Assert.True(result.Errors.Count >= 4);
        Assert.Equal(0, repository.Count);
    }

    [Fact]
    public void ImportFromLines_EmptyOptionalFields_AreAllowed()
    {
        var (repository, service) = CreateService();
        var lines = new[] { Header, CorsairRow };

        var result = service.ImportFromLines(lines);

        Assert.Equal(1, result.ImportedCount);
        Assert.Empty(result.Errors);

        var product = repository.GetBySku("1011");
        Assert.NotNull(product);
        Assert.Equal(string.Empty, product!.Description);
        Assert.Equal(string.Empty, product.ImageUrl);
    }

    [Fact]
    public void ImportFromLines_EmptyCurrency_IsReplacedWithRub()
    {
        var (repository, service) = CreateService();
        var lines = new[]
        {
            Header,
            "1011,Блок питания Corsair CV550,,4990,,Компьютеры,9,,true,Corsair,60,CV550"
        };

        var result = service.ImportFromLines(lines);

        Assert.Equal(1, result.ImportedCount);
        Assert.Equal("RUB", repository.GetBySku("1011")!.Currency);
    }

    [Fact]
    public void ImportFromLines_EmptyCategory_IsReplacedWithDefault()
    {
        var (repository, service) = CreateService();
        var lines = new[]
        {
            Header,
            "1011,Блок питания Corsair CV550,,4990,RUB,,9,,true,Corsair,60,CV550"
        };

        var result = service.ImportFromLines(lines);

        Assert.Equal(1, result.ImportedCount);
        Assert.Equal("Без категории", repository.GetBySku("1011")!.CategoryName);
    }

    [Fact]
    public void ImportFromLines_Cyrillic_IsPreserved()
    {
        var (repository, service) = CreateService();
        var lines = new[] { Header, LaptopRow };

        service.ImportFromLines(lines);

        var product = repository.GetBySku("1001");
        Assert.NotNull(product);
        Assert.Equal("Ноутбук Lenovo IdeaPad 3", product!.Name);
        Assert.Equal("Бюджетный ноутбук для работы и учебы", product.Description);
        Assert.Equal("Компьютеры", product.CategoryName);
    }
}
