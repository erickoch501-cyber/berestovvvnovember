namespace ECommerceCsvImport.Models;

/// <summary>
/// Ошибка импорта: номер строки файла и описание проблемы.
/// </summary>
public class ImportError
{
    public int RowNumber { get; set; }
    public string Message { get; set; } = string.Empty;
}
