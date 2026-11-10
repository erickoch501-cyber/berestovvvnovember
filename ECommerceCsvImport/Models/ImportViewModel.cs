namespace ECommerceCsvImport.Models;

/// <summary>
/// Модель представления страницы импорта.
/// </summary>
public class ImportViewModel
{
    public string FilePath { get; set; } = string.Empty;
    public ImportResult? Result { get; set; }
}
