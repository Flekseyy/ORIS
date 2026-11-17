using System.Text;

namespace MyHttpServer.MiniTemplateEngine.LanguageStructure;

/// <summary>
/// Разбивает шаблон на фрагменты: текст, выражения, заголовки if / else if / else / foreach и скобки блоков.
/// Ошибки не выбрасываются, а возвращаются фрагментами Error: так лексер не зависит от парсера.
/// </summary>
/// <remarks>
/// Правила: $$ даёт один $; $ перед символом, который не начинает имя, остаётся текстом ("5$");
/// else и else if распознаются сразу после закрывающей '}' блока if, без знака $.
/// Фигурные скобки в тексте внутри блока должны быть парными.
/// </remarks>
public sealed class TemplateLexer(string source)
{
    private readonly string _src = source ?? throw new ArgumentNullException(nameof(source));
    private readonly LineMap _map = new(source);
    private readonly List<Fragments> _fragments = [];
    private readonly Stack<Block> _blocks = new();

  
    private int _textStart = -1;
    private int _pos;
    private bool _done;

    /// <summary>Открытый блок: нужен, чтобы отличить скобку блока от скобки в тексте.</summary>
    private sealed class Block(bool isIf, int openAt)
    {
        /// <summary>Блок if: после его закрытия может идти else.</summary>
        public bool IsIf { get; } = isIf;

        /// <summary>Смещение открывающей '{' для сообщения об ошибке.</summary>
        public int OpenAt { get; } = openAt;

        /// <summary>Сколько '{' из текста внутри блока ещё не закрыто.</summary>
        public int LiteralBraces { get; set; }
    }

    /// <summary>Разбирает шаблон. Последним всегда идёт фрагмент EndOfFragment. Повторный вызов возвращает тот же результат.</summary>
    public IReadOnlyList<Fragments> Crushing()
    {
        if (_done) return _fragments;
        _done = true;

        while (_pos < _src.Length)
        {
            ReadPlainText();
            if (_pos >= _src.Length) break;

            switch (_src[_pos])
            {
                case '$': ReadDollar(); break;
                case '{': ReadOpenBrace(); break;
                default: ReadCloseBrace(); break;
            }
        }

        FlushText(_src.Length);

        foreach (var unclosed in _blocks)
            AddError("Не найдена закрывающая '}' блока", unclosed.OpenAt, 1);

        _fragments.Add(new Fragments(FragmentType.EndOfFragment, ReadOnlyMemory<char>.Empty, new SourceSpan(_map.Locate(_src.Length), 0)));
        return _fragments;
    }

    /// <summary>Читает текст до ближайшего '$', '{' или '}'.</summary>
    private void ReadPlainText()
    {
        var start = _pos;
        while (_pos < _src.Length && _src[_pos] is not ('$' or '{' or '}')) _pos++;
        if (_pos > start) StartText(start);
    }
    
   
    private void ReadDollar()
    {
        var pos = _pos;
        var next = pos + 1;

       
        if (next >= _src.Length || !(_src[next] == '$' || IsIdentStart(_src[next])))
        {
            StartText(pos);
            _pos = next;
            return;
        }
        
        if (_src[next] == '$')
        {
            StartText(pos);
            FlushText(pos + 1);
            _pos = next + 1;
            return;
        }

        ReadConstruct(pos);
    }

    private void ReadConstruct(int pos)
    {
        var idEnd = ScanEnd(pos + 1);
        var word = _src.AsSpan(pos + 1, idEnd - pos - 1);

        switch (word)
        {
            case "if":
                ReadCondition(pos, idEnd, FragmentType.If, isIf: true);
                break;
            case "foreach":
                ReadCondition(pos, idEnd, FragmentType.Foreach, isIf: false);
                break;
            case "raw":
                ReadImplicitExpression(pos, idEnd, allowCall: true);
                break;
            default:
                ReadImplicitExpression(pos, idEnd, allowCall: false);
                break;
        }
    }

    /// <summary>Читает заголовок вида $if () или $foreach () и следующую за ним '{'.</summary>
    private void ReadCondition(int pos, int idEnd, FragmentType kind, bool isIf)
    {
        FlushText(pos);

        var open = SkipWhitespace(idEnd);
        if (open >= _src.Length || _src[open] != '(')
        {
            AddError($"Ожидалась '(' после '${_src.AsSpan(pos + 1, idEnd - pos - 1)}'", pos, idEnd - pos);
            _pos = idEnd;
            return;
        }

        var end = ScanBalanced(open, '(', ')');
        if (end < 0)
        {
            AddError("Не найдена закрывающая ')'", pos, _src.Length - pos);
            _pos = _src.Length;
            return;
        }

        Add(kind, open + 1, end - open - 2);
        _pos = end;
        ExpectBlockOpen(isIf);
    }

    /// <summary>
    /// Читает $Имя, $Имя.Свойство, $Имя[индекс] и $raw(выражение).
    /// Для остальных имён скобка после имени остаётся обычным текстом: "$Name(x)" - это значение Name и текст "(x)".
    /// </summary>
    private void ReadImplicitExpression(int pos, int idEnd, bool allowCall)
    {
        var p = idEnd;

        if (allowCall && p < _src.Length && _src[p] == '(')
        {
            var end = ScanBalanced(p, '(', ')');
            if (end < 0)
            {
                FlushText(pos);
                AddError("Не найдена закрывающая ')' в выражении", pos, _src.Length - pos);
                _pos = _src.Length;
                return;
            }
            
            FlushText(pos);
            Add(FragmentType.Expression, pos + 1, end - pos - 1);
            _pos = end;
            return;
        }

        while (p < _src.Length)
        {
            if (_src[p] == '.' && p + 1 < _src.Length && IsIdentStart(_src[p + 1]))
            {
                p = ScanEnd(p + 1);
            }
            else if (_src[p] == '[')
            {
                var end = ScanBalanced(p, '[', ']');
                if (end < 0)
                {
                    FlushText(pos);
                    AddError("Не найдена закрывающая ']' в выражении", pos, _src.Length - pos);
                    _pos = _src.Length;
                    return;
                }

                p = end;
            }
            else
            {
                break;
            }
        }

        FlushText(pos);
        Add(FragmentType.Expression, pos + 1, p - pos - 1);
        _pos = p;
    }

    /// <summary>Требует '{' после заголовка и открывает блок.</summary>
    private void ExpectBlockOpen(bool isIf)
    {
        var p = SkipWhitespace(_pos);
        if (p < _src.Length && _src[p] == '{')
        {
            Add(FragmentType.BlockOpen, p, 1);
            _blocks.Push(new Block(isIf, p));
            _pos = p + 1;
            return;
        }

        AddError("Ожидалась '{' для начала блока", p, 1);
    }

    private void ReadOpenBrace()
    {
        if (_blocks.Count > 0) _blocks.Peek().LiteralBraces++;
        StartText(_pos);
        _pos++;
    }

    private void ReadCloseBrace()
    {
        var pos = _pos;
        if (_blocks.Count == 0)
        {
            StartText(pos);
            _pos++;
            return;
        }

        var block = _blocks.Peek();
        if (block.LiteralBraces > 0)
        {
            block.LiteralBraces--;
            StartText(pos);
            _pos++;
            return;
        }

        FlushText(pos);
        _blocks.Pop();
        _pos++;
        Add(FragmentType.BlockClose, pos, 1);
        if (block.IsIf)
        {
            TryReadElse();
        }
    }


    /// <summary>После '}' блока if ищет else или else if. Пробелы между ними пропускаются и в вывод не попадают.</summary>
    private void TryReadElse()
    {
        var p = SkipWhitespace(_pos);
        if (!StartsWithWord(p, "else")) return;

        var q = SkipWhitespace(p + 4);
        if (StartsWithWord(q, "if"))
        {
            var open = SkipWhitespace(q + 2);
            if (open < _src.Length && _src[open] == '(')
            {
                var end = ScanBalanced(open, '(', ')');
                if (end < 0)
                {
                    AddError("Не найдена закрывающая ')'", p, _src.Length - p);
                    _pos = _src.Length;
                    return;
                }

                Add(FragmentType.ElseIf, open + 1, end - open - 2);
                _pos = end;
                ExpectBlockOpen(isIf: true);
                return;
            }
        }

        if (q >= _src.Length || _src[q] != '{') return;
        Add(FragmentType.Else, p, 4);
        _pos = q;
        ExpectBlockOpen(isIf: false);
    }

   /// <summary>Запоминает начало текста, если он ещё не начался.</summary>
    private void StartText(int pos)
    {
        if (_textStart < 0) _textStart = pos;
    }

    /// <summary>Превращает накопленный текст (срез исходной строки до end) во фрагмент Text.</summary>
    private void FlushText(int end)
    {
        if (_textStart < 0) return;
        if (end > _textStart) Add(FragmentType.Text, _textStart, end - _textStart);
        _textStart = -1;
    }

    /// <summary>
    /// От открывающей скобки по open находит парную закрывающую и возвращает смещение после неё (или -1).
    /// Скобки внутри строк в кавычках не считаются.
    /// </summary>
    private int ScanBalanced(int open, char openChar, char closeChar)
    {
        var depth = 0;
        for (var i = open; i < _src.Length; i++)
        {
            var ch = _src[i];
            if (ch == '"')
            {
                var close = SkipString(i);
                if (close < 0) return -1;
                i = close;
            }
            else if (ch == openChar) depth++;
            else if (ch == closeChar)
            {
                depth--;
                if (depth == 0) return i + 1;
            }
        }

        return -1;
    }

    /// <summary>Возвращает смещение закрывающей кавычки строки, начавшейся в quote, или -1.</summary>
    private int SkipString(int quote)
    {
        for (var j = quote + 1; j < _src.Length; j++)
        {
            switch (_src[j])
            {
                case '\\':
                    j++;
                    break;
                case '"':
                    return j;
            }
        }

        return -1;
    }

    /// <summary>Возвращает смещение сразу после имени, которое начинается в p.</summary>
    private int ScanEnd(int p)
    {
        if (p >= _src.Length || !IsIdentStart(_src[p])) return p;

        p++;
        while (p < _src.Length && IsIdentPart(_src[p])) p++;
        return p;
    }

    private int SkipWhitespace(int p)
    {
        while (p < _src.Length && char.IsWhiteSpace(_src[p])) p++;
        return p;
    }

    private bool StartsWithWord(int p, string word)
    {
        if (p + word.Length > _src.Length || string.CompareOrdinal(_src, p, word, 0, word.Length) != 0)
            return false;

        var after = p + word.Length;
        return after >= _src.Length || !IsIdentPart(_src[after]);
    }

    private static bool IsIdentStart(char c) => char.IsLetter(c) || c == '_';

    private static bool IsIdentPart(char c) => char.IsLetterOrDigit(c) || c == '_';

    private void Add(FragmentType kind, int start, int length)
    {
        _fragments.Add(new Fragments(kind, _src.AsMemory(start, length), new SourceSpan(_map.Locate(start), length)));
    }

    private void AddError(string message, int start, int length)
    {
        start = Math.Min(start, _src.Length);
        var len = Math.Max(0, Math.Min(length, _src.Length - start));
        _fragments.Add(new Fragments(FragmentType.Error, message.AsMemory(), new SourceSpan(_map.Locate(start), len)));
    }
}
