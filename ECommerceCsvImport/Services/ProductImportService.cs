using System.Globalization;
using System.Text;
using ECommerceCsvImport.Models;

namespace ECommerceCsvImport.Services;

/// <summary>
/// Сервис импорта товаров из CSV-файла (вариант 1 "Электроника").
/// Читает строки, проверяет данные и добавляет корректные товары в репозиторий.
/// </summary>
public class ProductImportService : IProductImportService
{
    private const string DefaultCurrency = "RUB";
    private const string DefaultCategory = "Без категории";
    private const int MinWarrantyMonths = 0;
    private const int MaxWarrantyMonths = 60;
    private const int WindowsCyrillicCodePage = 1251;

    private const string FileNotFoundMessage = "Файл не найден";
    private const string FileReadErrorMessage = "Не удалось прочитать файл";
    private const string NoDataMessage = "Данные для импорта отсутствуют";
    private const string InvalidHeaderMessage = "Неверные заголовки CSV";
    private const string WrongFieldCountMessage = "Неверное количество полей в строке";
    private const string DuplicateSkuMessage = "Артикул товара уже существует";
    private const string EmptySkuMessage = "Поле sku не может быть пустым";
    private const string EmptyNameMessage = "Поле name не может быть пустым";
    private const string InvalidPriceMessage = "Поле price должно быть числом больше 0";
    private const string InvalidStockMessage = "Остаток на складе должен быть целым числом не меньше 0";
    private const string InvalidActiveMessage = "Поле is_active должно иметь значение true, false, 1 или 0";
    private const string EmptyBrandMessage = "Поле brand обязательно для варианта «Электроника»";
    private const string InvalidWarrantyMessage = "Гарантия должна быть целым числом от 0 до 60 месяцев";

    private static readonly string[] RequiredColumns =
    {
        "sku", "name", "description", "price", "currency", "category_name",
        "stock", "image_url", "is_active", "brand", "warranty_months", "model_number"
    };

    private readonly IProductRepository _repository;

    static ProductImportService()
    {
        // Нужно, чтобы .NET мог читать файлы в кодировке Windows-1251
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }

    public ProductImportService(IProductRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    /// <summary>
    /// Импортирует товары из CSV-файла по указанному пути.
    /// Ошибки файла не приводят к исключению, а попадают в результат импорта.
    /// </summary>
    public ImportResult ImportFromCsv(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
            return CreateFileError(FileNotFoundMessage);

        string[] lines;
        try
        {
            lines = ReadLines(filePath);
        }
        catch (IOException)
        {
            return CreateFileError(FileReadErrorMessage);
        }
        catch (UnauthorizedAccessException)
        {
            return CreateFileError(FileReadErrorMessage);
        }

        return ImportFromLines(lines);
    }

    /// <summary>
    /// Основная логика импорта: заголовок, разбор строк, валидация, добавление в репозиторий.
    /// Не зависит от файловой системы, поэтому удобна для unit-тестов.
    /// </summary>
    public ImportResult ImportFromLines(IEnumerable<string> lines)
    {
        var result = new ImportResult();
        var allLines = (lines ?? Enumerable.Empty<string>()).ToList();

        int headerIndex = allLines.FindIndex(line => !string.IsNullOrWhiteSpace(line));
        if (headerIndex < 0)
        {
            AddError(result, 0, NoDataMessage);
            return result;
        }

        int headerRowNumber = headerIndex + 1;
        IReadOnlyList<string> headerFields = CsvLineParser.Parse(allLines[headerIndex]);
        Dictionary<string, int> columnMap = BuildColumnMap(headerFields);

        if (!RequiredColumns.All(columnMap.ContainsKey))
        {
            AddError(result, headerRowNumber, InvalidHeaderMessage);
            return result;
        }

        int expectedFieldCount = headerFields.Count;
        var importedSkus = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        for (int index = headerIndex + 1; index < allLines.Count; index++)
        {
            if (string.IsNullOrWhiteSpace(allLines[index]))
                continue;

            ProcessRow(result, allLines[index], index + 1, columnMap, expectedFieldCount, importedSkus);
        }

        if (result.TotalRows == 0 && result.Errors.Count == 0)
            AddError(result, 0, NoDataMessage);

        return result;
    }

    /// <summary>
    /// Обрабатывает одну непустую строку данных: разбор, валидация, проверка дубликата, добавление.
    /// </summary>
    private void ProcessRow(
        ImportResult result,
        string line,
        int rowNumber,
        IReadOnlyDictionary<string, int> columnMap,
        int expectedFieldCount,
        HashSet<string> importedSkus)
    {
        result.TotalRows++;

        IReadOnlyList<string> values = CsvLineParser.Parse(line);
        if (values.Count != expectedFieldCount)
        {
            AddError(result, rowNumber, WrongFieldCountMessage);
            return;
        }

        var rowErrors = new List<ImportError>();
        Product? product = BuildProduct(values, columnMap, rowNumber, rowErrors);
        if (product == null)
        {
            result.Errors.AddRange(rowErrors);
            return;
        }

        bool isDuplicate = _repository.GetBySku(product.Sku) != null
                           || importedSkus.Contains(product.Sku);
        if (isDuplicate)
        {
            AddError(result, rowNumber, DuplicateSkuMessage);
            return;
        }

        _repository.Add(product);
        importedSkus.Add(product.Sku);
        result.ImportedCount++;
    }

    /// <summary>
    /// Проверяет поля строки и создает товар.
    /// Если есть ошибки, они добавляются в rowErrors и возвращается null.
    /// </summary>
    private static Product? BuildProduct(
        IReadOnlyList<string> values,
        IReadOnlyDictionary<string, int> columnMap,
        int rowNumber,
        List<ImportError> rowErrors)
    {
        string sku = GetValue(values, columnMap, "sku");
        if (string.IsNullOrWhiteSpace(sku))
            rowErrors.Add(CreateError(rowNumber, EmptySkuMessage));

        string name = GetValue(values, columnMap, "name");
        if (string.IsNullOrWhiteSpace(name))
            rowErrors.Add(CreateError(rowNumber, EmptyNameMessage));

        bool priceValid = TryParseDecimal(GetValue(values, columnMap, "price"), out decimal price)
                          && price > 0;
        if (!priceValid)
            rowErrors.Add(CreateError(rowNumber, InvalidPriceMessage));

        bool stockValid = TryParseInt(GetValue(values, columnMap, "stock"), out int stock)
                          && stock >= 0;
        if (!stockValid)
            rowErrors.Add(CreateError(rowNumber, InvalidStockMessage));

        bool activeValid = TryParseBool(GetValue(values, columnMap, "is_active"), out bool isActive);
        if (!activeValid)
            rowErrors.Add(CreateError(rowNumber, InvalidActiveMessage));

        string brand = GetValue(values, columnMap, "brand");
        if (string.IsNullOrWhiteSpace(brand))
            rowErrors.Add(CreateError(rowNumber, EmptyBrandMessage));

        bool warrantyValid = TryParseInt(GetValue(values, columnMap, "warranty_months"), out int warrantyMonths)
                             && warrantyMonths >= MinWarrantyMonths
                             && warrantyMonths <= MaxWarrantyMonths;
        if (!warrantyValid)
            rowErrors.Add(CreateError(rowNumber, InvalidWarrantyMessage));

        if (rowErrors.Count > 0)
            return null;

        return new Product
        {
            Sku = sku,
            Name = name,
            Description = GetValue(values, columnMap, "description"),
            Price = price,
            Currency = NormalizeCurrency(GetValue(values, columnMap, "currency")),
            CategoryName = NormalizeCategory(GetValue(values, columnMap, "category_name")),
            StockQuantity = stock,
            ImageUrl = GetValue(values, columnMap, "image_url"),
            IsActive = isActive,
            Brand = brand,
            WarrantyMonths = warrantyMonths,
            ModelNumber = GetValue(values, columnMap, "model_number")
        };
    }

    /// <summary>Читает файл, определяя кодировку: UTF-8 (с BOM или без) или Windows-1251.</summary>
    private static string[] ReadLines(string filePath)
    {
        byte[] bytes = File.ReadAllBytes(filePath);
        string text;

        try
        {
            text = new UTF8Encoding(false, true).GetString(bytes);
        }
        catch (DecoderFallbackException)
        {
            text = Encoding.GetEncoding(WindowsCyrillicCodePage).GetString(bytes);
        }

        return text.TrimStart('\uFEFF').Split(new[] { "\r\n", "\n", "\r" }, StringSplitOptions.None);
    }

    /// <summary>Строит словарь "имя колонки - индекс поля" без учета регистра, берет первое вхождение.</summary>
    private static Dictionary<string, int> BuildColumnMap(IReadOnlyList<string> headerFields)
    {
        var columnMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        for (int i = 0; i < headerFields.Count; i++)
        {
            string columnName = headerFields[i].Trim().TrimStart('\uFEFF');
            if (!columnMap.ContainsKey(columnName))
                columnMap[columnName] = i;
        }

        return columnMap;
    }

    /// <summary>Возвращает значение поля по имени колонки или пустую строку.</summary>
    private static string GetValue(
        IReadOnlyList<string> values,
        IReadOnlyDictionary<string, int> columnMap,
        string columnName)
    {
        if (columnMap.TryGetValue(columnName, out int index) && index < values.Count)
            return values[index].Trim();

        return string.Empty;
    }

    private static bool TryParseDecimal(string value, out decimal result)
    {
        return decimal.TryParse(
            value,
            NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint,
            CultureInfo.InvariantCulture,
            out result);
    }

    private static bool TryParseInt(string value, out int result)
    {
        return int.TryParse(
            value,
            NumberStyles.AllowLeadingSign,
            CultureInfo.InvariantCulture,
            out result);
    }

    /// <summary>Допустимые значения: true, false, 1, 0 (регистр не важен).</summary>
    private static bool TryParseBool(string value, out bool result)
    {
        string normalized = value.Trim();

        if (normalized.Equals("true", StringComparison.OrdinalIgnoreCase) || normalized == "1")
        {
            result = true;
            return true;
        }

        if (normalized.Equals("false", StringComparison.OrdinalIgnoreCase) || normalized == "0")
        {
            result = false;
            return true;
        }

        result = false;
        return false;
    }

    private static string NormalizeCurrency(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return DefaultCurrency;

        return value.Trim().ToUpperInvariant();
    }

    private static string NormalizeCategory(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return DefaultCategory;

        return value.Trim();
    }

    private static ImportError CreateError(int rowNumber, string message)
    {
        return new ImportError { RowNumber = rowNumber, Message = message };
    }

    private static void AddError(ImportResult result, int rowNumber, string message)
    {
        result.Errors.Add(CreateError(rowNumber, message));
    }

    private static ImportResult CreateFileError(string message)
    {
        var result = new ImportResult();
        AddError(result, 0, message);
        return result;
    }
}
