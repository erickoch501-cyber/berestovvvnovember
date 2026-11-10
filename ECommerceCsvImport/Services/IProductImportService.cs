using ECommerceCsvImport.Models;

namespace ECommerceCsvImport.Services;

/// <summary>
/// Сервис импорта товаров из CSV.
/// </summary>
public interface IProductImportService
{
    ImportResult ImportFromCsv(string filePath);
    ImportResult ImportFromLines(IEnumerable<string> lines);
}
