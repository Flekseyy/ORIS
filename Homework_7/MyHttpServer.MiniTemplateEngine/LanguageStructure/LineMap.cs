namespace MyHttpServer.MiniTemplateEngine.LanguageStructure;

/// <summary>
/// Переводит смещение в тексте в номер строки и столбца (оба с единицы). Нужна для сообщений об ошибках.
/// </summary>
internal sealed class LineMap
{
    // смещения начала каждой строки; первая строка начинается с нуля
    private readonly List<int> _lineStarts = [0];

    public LineMap(string text)
    {
        for (var i = 0; i < text.Length; i++)
        {
            if (text[i] == '\n')
            {
                _lineStarts.Add(i + 1);
            }
        }
    }

    /// <summary>Находит строку бинарным поиском: последнюю, которая начинается не позже offset.</summary>
    public SourceLocation Locate(int offset)
    {
        var low = 0;
        var high = _lineStarts.Count - 1;
        var lineIndex = 0;

        while (low <= high)
        {
            var middle = (low + high) / 2;
            if (_lineStarts[middle] <= offset)
            {
                lineIndex = middle;
                low = middle + 1;
            }
            else
            {
                high = middle - 1;
            }
        }

        return new SourceLocation(offset, lineIndex + 1, offset - _lineStarts[lineIndex] + 1);
    }
}
