using MyHttpServer.MiniTemplateEngine.AST;
using MyHttpServer.MiniTemplateEngine.Expressions;
using MyHttpServer.MiniTemplateEngine.LanguageStructure;

namespace MyHttpServer.MiniTemplateEngine.Syntax;

/// <summary>
/// Строит дерево шаблона из фрагментов лексера. Первая же ошибка (в том числе фрагмент Error)
/// заканчивается исключением TemplateSyntaxException.
/// </summary>
public sealed class TemplateParser
{
    private readonly IReadOnlyList<Fragments> _fragments;
    private int _index;

    private TemplateParser(IReadOnlyList<Fragments> fragments)
    {
        _fragments = fragments ?? throw new ArgumentNullException(nameof(fragments));
        if (fragments.Count == 0 || fragments[^1].FragmentType != FragmentType.EndOfFragment)
            throw new ArgumentException("Список фрагментов должен заканчиваться EndOfFragment", nameof(fragments));
    }

    private Fragments Current => _fragments[_index];

    public static TemplateNode Parse(string source)
    {
        ArgumentNullException.ThrowIfNull(source);
        var fragments = new TemplateLexer(source).Crushing();
        return new TemplateParser(fragments).ParseTemplate();
    }

    private TemplateNode ParseTemplate()
    {
        var body = ParseNodes(insideBlock: false);
        var span = new SourceSpan(new SourceLocation(0, 1, 1), Current.SourceSpan.Start.Offset);
        return new TemplateNode(body, span);
    }

    private List<Node> ParseNodes(bool insideBlock)
    {
        var nodes = new List<Node>();
        while (true)
        {
            var fragment = Current;
            switch (fragment.FragmentType)
            {
                case FragmentType.Text:
                    Advance();
                    nodes.Add(new TextNode(fragment.Value, fragment.SourceSpan));
                    break;
                case FragmentType.Expression:
                    Advance();
                    nodes.Add(ParseOutput(fragment));
                    break;
                case FragmentType.If:
                    nodes.Add(ParseIf());
                    break;
                case FragmentType.Foreach:
                    nodes.Add(ParseForeach());
                    break;
                case FragmentType.BlockClose:
                    return insideBlock ? nodes : throw Error(fragment, "Лишняя '}'");
                case FragmentType.EndOfFragment:
                    return insideBlock ? throw Error(fragment, "Неожиданный конец шаблона: не найдена '}'") : nodes;
                case FragmentType.Error:
                    throw Error(fragment, fragment.Value.Span.ToString());
                default:
                    throw Error(fragment, $"Неожиданный фрагмент {fragment.FragmentType}");
            }
        }
    }

    private IfNode ParseIf()
    {
        var start = Current;
        var branches = new List<IfBranch> { ParseBranch(Advance()) };
        while (Current.FragmentType == FragmentType.ElseIf) branches.Add(ParseBranch(Advance()));
        List<Node>? elseBody = null;
        if (Current.FragmentType == FragmentType.Else) { Advance(); elseBody = ParseBlock(); }
        return new IfNode(branches, elseBody, start.SourceSpan);
    }

    private IfBranch ParseBranch(Fragments header)
    {
        var condition = InExpression(header, () => ExprParser.Parse(header.Value));
        var body = ParseBlock();
        return new IfBranch(condition, body, header.SourceSpan);
    }

    private ForeachNode ParseForeach()
    {
        var header = Advance();
        var (name, collection) = InExpression(header, () => ParseForeachHeader(header.Value));
        var body = ParseBlock();
        return new ForeachNode(name, collection, body, header.SourceSpan);
    }

    private static (string Name, Expr Collection) ParseForeachHeader(ReadOnlyMemory<char> text)
    {
        var parser = new ExprParser(new ExprLexer(text).Crushing());
        var name = parser.Expect(ExprType.Identifier, "имя переменной").Text.Span.ToString();
        if (name == "var") name = parser.Expect(ExprType.Identifier, "имя переменной").Text.Span.ToString();
        var keyword = parser.Expect(ExprType.Identifier, "'in'");
        if (keyword.Text.Span.ToString() != "in") throw new ExpressionException("Ожидалось 'in'", keyword.Position);
        var collection = parser.ParseExpression();
        if (!parser.AtEnd)
            throw new ExpressionException($"Лишний текст '{parser.Current.Text.Span.ToString()}'", parser.Current.Position);
        return (name, collection);
    }

    private static OutputNode ParseOutput(Fragments fragment)
    {
        var expression = InExpression(fragment, () => ExprParser.Parse(fragment.Value));
        return new OutputNode(expression, fragment.SourceSpan);
    }

    private List<Node> ParseBlock()
    {
        Expect(FragmentType.BlockOpen, "'{'");
        var body = ParseNodes(insideBlock: true);
        Expect(FragmentType.BlockClose, "'}'");
        return body;
    }

    private Fragments Advance()
    {
        var fragment = Current;
        if (fragment.FragmentType != FragmentType.EndOfFragment) _index++;
        return fragment;
    }

    private void Expect(FragmentType type, string description)
    {
        var fragment = Current;
        if (fragment.FragmentType == type) { Advance(); return; }
        throw fragment.FragmentType == FragmentType.Error
            ? Error(fragment, fragment.Value.Span.ToString())
            : Error(fragment, $"Ожидалось {description}, найдено {Describe(fragment)}");
    }

    private static string Describe(Fragments fragment)
        => fragment.FragmentType == FragmentType.EndOfFragment ? "конец шаблона" : fragment.FragmentType.ToString();

    private static T InExpression<T>(Fragments fragment, Func<T> action)
    {
        try { return action(); }
        catch (ExpressionException ex)
        {
            throw new TemplateSyntaxException($"{ex.Message} (в выражении '{fragment.Value.Span.ToString().Trim()}')", fragment.SourceSpan.Start);
        }
    }

    private static TemplateSyntaxException Error(Fragments fragment, string message)
        => new(message, fragment.SourceSpan.Start);
}