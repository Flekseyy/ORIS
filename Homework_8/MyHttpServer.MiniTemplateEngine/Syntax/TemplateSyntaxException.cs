using MyHttpServer.MiniTemplateEngine.LanguageStructure;

namespace MyHttpServer.MiniTemplateEngine.Syntax;

/// <summary>Синтаксическая ошибка шаблона. В сообщение добавляются строка и столбец.</summary>
public sealed class TemplateSyntaxException(string message, SourceLocation location)
    : Exception($"{message} (строка {location.Line}, столбец {location.Column})");
