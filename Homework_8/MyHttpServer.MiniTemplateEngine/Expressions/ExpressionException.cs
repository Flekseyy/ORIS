namespace MyHttpServer.MiniTemplateEngine.Expressions;

/// <summary>Ошибка разбора или вычисления выражения. Position - смещение в тексте выражения (0, если место неизвестно).</summary>
public sealed class ExpressionException(string message, int position) : Exception(message)
{
    /// <summary>Смещение места ошибки в тексте выражения.</summary>
    public int Position { get; } = position;
}