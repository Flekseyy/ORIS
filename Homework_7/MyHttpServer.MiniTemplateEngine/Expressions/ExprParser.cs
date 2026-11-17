namespace MyHttpServer.MiniTemplateEngine.Expressions;

/// <summary>
/// Строит дерево выражения из фрагментов методом рекурсивного спуска.
/// Приоритеты от низшего к высшему
/// </summary>
public sealed class ExprParser
{
    
    private static readonly Dictionary<ExprType, Operation> OrOperators = new() { [ExprType.Or] = Operation.Or };
    private static readonly Dictionary<ExprType, Operation> AndOperators = new() { [ExprType.And] = Operation.And };
    private static readonly Dictionary<ExprType, Operation> EqualityOperators = new() { [ExprType.Eq] = Operation.Eq, [ExprType.NoEq] = Operation.Neq };
    private static readonly Dictionary<ExprType, Operation> RelationalOperators = new()
    {
        [ExprType.Lt] = Operation.Lt, [ExprType.Gt] = Operation.Gt,
        [ExprType.Le] = Operation.Le, [ExprType.Ge] = Operation.Ge,
    };

    private readonly IReadOnlyList<ExprFragment> _fragments;
    private int _index;

    /// <summary>
    /// Создание ExprParser
    /// </summary>
    /// <param name="fragments"></param>
    /// <exception cref="ArgumentNullException"></exception>
    /// <exception cref="ArgumentException"></exception>
    public ExprParser(IReadOnlyList<ExprFragment> fragments)
    {
        _fragments = fragments ?? throw new ArgumentNullException(nameof(fragments));
        if (fragments.Count == 0 || fragments[^1].Kind != ExprType.EndOfFrag)
            throw new ArgumentException("Список фрагментов должен заканчиваться EndOfFrag", nameof(fragments));
    }

    /// <summary>Текущий фрагмент.</summary>
    public ExprFragment Current => _fragments[_index];

    /// <summary>True, если все фрагменты прочитаны.</summary>
    public bool AtEnd => Current.Kind == ExprType.EndOfFrag;

    /// <summary>Разбирает строку целиком. Лишние символы после выражения считаются ошибкой.</summary>
    /// <exception cref="ExpressionException">Ошибка в тексте выражения.</exception>
    public static Expr Parse(string source)
    {
        ArgumentNullException.ThrowIfNull(source);
        return Parse(source.AsMemory());
    }
    
    /// <summary>Разбирает текст выражения целиком. Лишние символы после выражения считаются ошибкой.</summary>
    /// <exception cref="ExpressionException">Ошибка в тексте выражения.</exception>
    public static Expr Parse(ReadOnlyMemory<char> source)
    {
        var parser = new ExprParser(new ExprLexer(source).Crushing());
        var expr = parser.ParseExpression();
        return parser.AtEnd ? expr : throw parser.Unexpected();
    }
    
    /// <summary>Разбирает одно выражение с текущей позиции и останавливается, не проверяя конец.</summary>
    public Expr ParseExpression() => ParseOr();

    /// <summary>Требует фрагмент заданного вида и переходит к следующему.</summary>
    public ExprFragment Expect(ExprType type, string description)
    {
        return Current.Kind == type ? Advance() : throw Unexpected(description);
    }



    private ExprFragment Advance()
    {
        var fragment = Current;
        if (fragment.Kind != ExprType.EndOfFrag)
        {
            _index++;
        }

        return fragment;
    }

    private Expr ParseOr() => ParseLevel(ParseAnd, OrOperators);

    private Expr ParseAnd() => ParseLevel(ParseEquality, AndOperators);

    private Expr ParseEquality() => ParseLevel(ParseRelational, EqualityOperators);

    private Expr ParseRelational() => ParseLevel(ParseUnary, RelationalOperators);


    /// <summary>
    /// Общий разбор одного уровня приоритета с левой ассоциативностью
    /// </summary>
    private Expr ParseLevel(Func<Expr> parseOperand, Dictionary<ExprType, Operation> operators)
    {
        var left = parseOperand();
        while (operators.TryGetValue(Current.Kind, out var operation))
        {
            var token = Advance();
            var right = parseOperand();
            left = new BinaryExpr(operation, left, right, token.Position);
        }

        return left;
    }

    private Expr ParseUnary()
    {
        if (Current.Kind is not (ExprType.Not or ExprType.Minus)) return ParsePostfix();

        var token = Advance();
        var operation = token.Kind == ExprType.Not ? Operation.Not : Operation.Minus;
        return new UnaryExpr(operation, ParseUnary(), token.Position);
    }

    private Expr ParsePostfix()
    {
        var expr = ParsePrimary();

        while (true)
        {
            switch (Current.Kind)
            {
                case ExprType.Dot:
                {
                    Advance();
                    var name = Expect(ExprType.Identifier, "имя члена");
                    expr = new MemberExpr(expr, name.Text.Span.ToString(), name.Position);
                    break;
                }
                case ExprType.LBracket:
                {
                    var open = Advance();
                    var index = ParseExpression();
                    Expect(ExprType.RBracket, "']'");
                    expr = new IndexExpr(expr, index, open.Position);
                    break;
                }
                default:
                    return expr;
            }
        }
    }

    private Expr ParsePrimary()
    {
        var fragment = Current;
        switch (fragment.Kind)
        {
            case ExprType.Number:
            case ExprType.String:
                Advance();
                return new LiteralExpr(fragment.Value, fragment.Position);

            case ExprType.Identifier:
                Advance();
                var name = fragment.Text.Span.ToString();
                return name switch
                {
                    "true" => new LiteralExpr(true, fragment.Position),
                    "false" => new LiteralExpr(false, fragment.Position),
                    "null" => new LiteralExpr(null, fragment.Position),
                    "raw" => ParseRawCall(fragment.Position),
                    _ => new IdentifierExpr(name, fragment.Position),
                };

            case ExprType.LParen:
                Advance();
                var inner = ParseExpression();
                Expect(ExprType.RParen, "')'");
                return inner;

            default:
                throw Unexpected();
        }
    }


    /// <summary>Разбирает raw</summary>
    private Expr ParseRawCall(int position)
    {
        Expect(ExprType.LParen, "'(' после 'raw'");
        var inner = ParseExpression();
        Expect(ExprType.RParen, "')'");
        return new RawExpr(inner, position);
    }

    private ExpressionException Unexpected(string? expected = null)
    {
        var fragment = Current;
        var found = fragment.Kind == ExprType.EndOfFrag ? "конец выражения" : $"'{fragment.Text.Span.ToString()}'";
        var message = expected is null ? $"Неожиданно: {found}" : $"Ожидалось {expected}, найдено {found}";
        return new ExpressionException(message, fragment.Position);
    }
}
