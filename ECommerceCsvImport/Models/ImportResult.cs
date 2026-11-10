namespace ECommerceCsvImport.Models;

/// <summary>
/// Результат импорта: количество строк, успешно загруженных товаров и список ошибок.
/// </summary>
public class ImportResult
{
    public int TotalRows { get; set; }
    public int ImportedCount { get; set; }
    public List<ImportError> Errors { get; set; } = new();
    public int ErrorCount => Errors.Count;
}
