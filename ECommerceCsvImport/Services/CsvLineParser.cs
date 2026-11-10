using System.Text;

namespace ECommerceCsvImport.Services;

/// <summary>
/// Разбор одной строки CSV с поддержкой пустых полей, кавычек и запятых внутри кавычек.
/// </summary>
public static class CsvLineParser
{
    private const char Separator = ',';
    private const char Quote = '"';

    /// <summary>
    /// Разбивает строку CSV на поля. Двойные кавычки внутри кавычек ("") превращаются в одну.
    /// Каждое поле обрезается по краям (trim).
    /// </summary>
    public static IReadOnlyList<string> Parse(string line)
    {
        var fields = new List<string>();

        if (line == null)
            return fields;

        var current = new StringBuilder();
        bool inQuotes = false;

        for (int i = 0; i < line.Length; i++)
        {
            char symbol = line[i];

            if (inQuotes)
            {
                if (symbol == Quote)
                {
                    bool isEscapedQuote = i + 1 < line.Length && line[i + 1] == Quote;
                    if (isEscapedQuote)
                    {
                        current.Append(Quote);
                        i++;
                    }
                    else
                    {
                        inQuotes = false;
                    }
                }
                else
                {
                    current.Append(symbol);
                }
            }
            else if (symbol == Quote)
            {
                inQuotes = true;
            }
            else if (symbol == Separator)
            {
                fields.Add(current.ToString().Trim());
                current.Clear();
            }
            else
            {
                current.Append(symbol);
            }
        }

        fields.Add(current.ToString().Trim());
        return fields;
    }
}
