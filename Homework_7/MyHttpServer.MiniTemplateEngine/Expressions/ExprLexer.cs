using System.Globalization;
using System.Text;

namespace MyHttpServer.MiniTemplateEngine.Expressions;

/// <summary>
/// Разбивает текст выражения на фрагменты: имена, числа, строки, знаки
/// </summary>
public sealed class ExprLexer(ReadOnlyMemory<char> source)
{
    private readonly ReadOnlyMemory<char> _src = source;
    private ReadOnlySpan<char> SrcSpan => _src.Span;
    private int _position;


    /// <summary>Разбирает всё выражение. Последним всегда идёт фрагмент EndOfFrag.</summary>
    /// <exception cref="ExpressionException">Неизвестный символ, незакрытая строка, неверное число.</exception>
    public List<ExprFragment> Crushing()
    {
        var fragments = new List<ExprFragment>();
        ExprFragment fragment;
        do
        {
            fragment = Next();
            fragments.Add(fragment);
        } 
        while (fragment.Kind != ExprType.EndOfFrag);
        return fragments;
    }

    private ExprFragment Next()
    {
        SkipWhitespace();
        var start = _position;
        if (_position >= SrcSpan.Length) return Make(ExprType.EndOfFrag, start, 0);
        
        var c = SrcSpan[_position];
        if (IsIdentStart(c)) return ReadIdentifier(start);
        if (IsDigit(c)) return ReadNumber(start);
        if (c == '"') return ReadString(start);
        return ReadSymbol(c, start);
    }

    private ExprFragment ReadIdentifier(int start)
    {
        _position++;
        while (_position < SrcSpan.Length && IsIdentPart(SrcSpan[_position]))
        {
            _position++;
        }
        return Make(ExprType.Identifier, start, _position - start);
    }

    private ExprFragment ReadNumber(int start)
    {
        SkipDigits();
        var hasDot = false;
        if (_position + 1 < SrcSpan.Length && SrcSpan[_position] == '.' && IsDigit(SrcSpan[_position + 1]))
        {
            hasDot = true;
            _position++;
            SkipDigits();
        }
        
        var textSpan = SrcSpan.Slice(start, _position - start);
        object value;
        if (!hasDot && int.TryParse(textSpan, NumberStyles.None, CultureInfo.InvariantCulture, out var intValue))
        {
            value = intValue;
        }
        else if (decimal.TryParse(textSpan, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var decimalValue))
        {
            value = decimalValue;
        }
        else
        {
            throw new ExpressionException($"Некорректное число '{textSpan.ToString()}'", start);
        }
        return new ExprFragment(ExprType.Number, _src.Slice(start, _position - start), start, value);
    }

    private ExprFragment ReadString(int start)
    {
        _position++;
        var builder = new StringBuilder();
        while (_position < SrcSpan.Length)
        {
            var ch = SrcSpan[_position++];
            if (ch == '"')
                return new ExprFragment(ExprType.String, _src.Slice(start, _position - start), start, builder.ToString());
            if (ch == '\\' && _position < SrcSpan.Length)
            {
                builder.Append(Unescape(SrcSpan[_position++]));
                continue;
            }
            builder.Append(ch);
        }
        throw new ExpressionException("Незакрытая строка", start);
    }

    private ExprFragment ReadSymbol(char c, int start)
    {
        _position++;
        return c switch
        {
            '.' => Make(ExprType.Dot, start, 1),
            '(' => Make(ExprType.LParen, start, 1),
            ')' => Make(ExprType.RParen, start, 1),
            '[' => Make(ExprType.LBracket, start, 1),
            ']' => Make(ExprType.RBracket, start, 1),
            '-' => Make(ExprType.Minus, start, 1),
            '!' => Take('=') ? Make(ExprType.NoEq, start, 2) : Make(ExprType.Not, start, 1),
            '<' => Take('=') ? Make(ExprType.Le, start, 2) : Make(ExprType.Lt, start, 1),
            '>' => Take('=') ? Make(ExprType.Ge, start, 2) : Make(ExprType.Gt, start, 1),
            '=' => Take('=') ? Make(ExprType.Eq, start, 2) : throw new ExpressionException("Ожидалось '=='", start),
            '&' => Take('&') ? Make(ExprType.And, start, 2) : throw new ExpressionException("Ожидалось '&&'", start),
            '|' => Take('|') ? Make(ExprType.Or, start, 2) : throw new ExpressionException("Ожидалось '||'", start),
            _ => throw new ExpressionException($"Неизвестный символ '{c}'", start),
        };
    }

    private void SkipWhitespace()
    {
        while (_position < SrcSpan.Length && char.IsWhiteSpace(SrcSpan[_position])) _position++;
    }

    private void SkipDigits()
    {
        while (_position < SrcSpan.Length && IsDigit(SrcSpan[_position])) _position++;
    }

    private bool Take(char expected)
    {
        if (_position >= SrcSpan.Length || SrcSpan[_position] != expected) return false;
        _position++;
        return true;
    }

    private static char Unescape(char c) => c switch { 'n' => '\n', 't' => '\t', 'r' => '\r', '\\' => '\\', '"' => '"', _ => c };


    private ExprFragment Make(ExprType type, int start, int length) => new(type, _src.Slice(start, length), start);

    private static bool IsDigit(char c) => c is >= '0' and <= '9';

    private static bool IsIdentStart(char c) => char.IsLetter(c) || c == '_';

    private static bool IsIdentPart(char c) => char.IsLetterOrDigit(c) || c == '_';
}
