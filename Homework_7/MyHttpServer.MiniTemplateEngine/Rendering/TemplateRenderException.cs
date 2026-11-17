using MyHttpServer.MiniTemplateEngine.LanguageStructure;

namespace MyHttpServer.MiniTemplateEngine.Rendering;

/// <summary>Ошибка при выполнении шаблона. В сообщение добавляются строка и столбец узла.</summary>
public sealed class TemplateRenderException(string message, SourceSpan span, Exception? innerException = null)
    : Exception($"{message} (строка {span.Start.Line}, столбец {span.Start.Column})", innerException);
