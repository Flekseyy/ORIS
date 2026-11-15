namespace MyHttpServer.MiniTemplateEngine;

/// <summary>
/// Базовое исключение движка шаблонов.
/// </summary>
public class TemplateException(string message, Exception inner) : Exception(message, inner);