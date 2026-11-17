using System.Collections;
using System.Globalization;
using System.Net;
using System.Text;
using MyHttpServer.MiniTemplateEngine.AST;
using MyHttpServer.MiniTemplateEngine.Expressions;
using MyHttpServer.MiniTemplateEngine.LanguageStructure;
using MyHttpServer.MiniTemplateEngine.Rendering;

namespace MyHttpServer.MiniTemplateEngine.Rendering;


/// <summary>
/// Выполняет дерево шаблона: обходит узлы и собирает результат в строку.
/// Значения форматируются в инвариантной культуре, null выводится как пустая строка.
/// </summary>
internal static class Renderer
{
    /// <summary>Выполняет шаблон для модели.</summary>
    /// <exception cref="TemplateRenderException">Ошибка в выражении или цикл по значению, которое нельзя перебрать.</exception>
    public static string Render(TemplateNode tree, object model)
    {
        var context = new RenderContext(model);
        var output = new StringBuilder();
        RenderNodes(tree.Body, context, output);
        return output.ToString();
    }

    private static void RenderNodes(IReadOnlyList<Node> nodes, RenderContext context, StringBuilder output)
    {
        foreach (var node in nodes)
        {
            RenderNode(node, context, output);
        }
    }

    private static void RenderNode(Node node, RenderContext context, StringBuilder output)
    {
        switch (node)
        {
            case TextNode textNode:
                output.Append(textNode.Text.Span);
                break;
            case OutputNode print:
                var value = Evaluate(print.Expression, context, print.Span);
                var text = ToText(value);
                output.Append(print.Expression is RawExpr ? text : WebUtility.HtmlEncode(text));
                break;
            case IfNode conditional:
                RenderIf(conditional, context, output);
                break;
            case ForeachNode loop:
                RenderForeach(loop, context, output);
                break;
            default:
                throw new InvalidOperationException($"Неизвестный узел {node.GetType().Name}");
        }
    }

    private static void RenderIf(IfNode node, RenderContext context, StringBuilder output)
    {
        foreach (var branch in node.Branches)
        {
            if (ExprEvaluator.IsTruthy(Evaluate(branch.Condition, context, branch.Span)))
            {
                RenderNodes(branch.Body, context, output);
                return;
            }
        }
        if (node.ElseBody is not null) RenderNodes(node.ElseBody, context, output);
    }

    private static void RenderForeach(ForeachNode node, RenderContext context, StringBuilder output)
    {
        var collection = Evaluate(node.Collection, context, node.Span);
        if (collection is null) throw new TemplateRenderException("Нельзя перебрать null", node.Span);
        if (collection is not IEnumerable items)
            throw new TemplateRenderException($"Нельзя перебрать значение типа '{collection.GetType().Name}'", node.Span);

        foreach (var item in items)
        {
            context.PushVariable(node.VariableName, item);
            try { RenderNodes(node.Body, context, output); }
            finally { context.PopVariable(); }
        }
    }

    /// <summary>Вычисляет выражение; любую ошибку превращает в TemplateRenderException с местом в шаблоне.</summary>
    private static object? Evaluate(Expr expression, RenderContext context, SourceSpan span)
    {
        try
        {
            return ExprEvaluator.Evaluate(expression, context);
        }
        catch (ExpressionException ex)
        {
            throw new TemplateRenderException(ex.Message, span, ex);
        }
        catch (Exception ex)
        {
            throw new TemplateRenderException($"{ex.GetType().Name}: {ex.Message}", span, ex);
        }
    }


    private static string ToText(object? value)
    {
        return value switch
        {
            null => string.Empty,
            string text => text,
            IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
            _ => value.ToString() ?? string.Empty,
        };
    }
}
