namespace MyHttpServer.MiniTemplateEngine.Expressions;

public sealed class ExpressionException(string message, int position) : Exception(message);