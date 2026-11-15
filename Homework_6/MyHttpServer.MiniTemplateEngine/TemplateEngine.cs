using System.Collections.Concurrent;
using MyHttpServer.MiniTemplateEngine.AST;
using MyHttpServer.MiniTemplateEngine.Rendering;
using MyHttpServer.MiniTemplateEngine.Syntax;

namespace MyHttpServer.MiniTemplateEngine;

/// <summary>
/// Фасад шаблонизатора. Точка входа и запуск 
/// </summary>
public static class TemplateEngine
{
    /// <summary>
    /// Кэш скомпилированных шаблонов
    /// </summary>
    private static readonly ConcurrentDictionary<string, TemplateNode> TemplateCache = new();
    /// <summary>
    /// Рендерит текст шаблона с указанной моделью.
    /// </summary>
    public static string Render(string template, object model)
    {
        ArgumentNullException.ThrowIfNull(template);
        ArgumentNullException.ThrowIfNull(model);

        var tree = TemplateCache.GetOrAdd(template, t =>
        {
            try
            {
                return TemplateParser.Parse(t);
            }
            catch (TemplateSyntaxException ex) { throw new TemplateException($"Ошибка синтаксиса: {ex.Message}", ex); }
        });

        try
        {
            return Renderer.Render(tree, model);
        }
        catch (TemplateRenderException ex)
        {
            throw new TemplateException($"Ошибка рендеринга: {ex.Message}", ex);
        }
    }
    /// <summary>Очистка кэша</summary>
    public static void ClearCache() => TemplateCache.Clear();
}