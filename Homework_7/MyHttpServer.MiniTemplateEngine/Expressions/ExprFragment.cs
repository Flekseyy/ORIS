namespace MyHttpServer.MiniTemplateEngine.Expressions;

public readonly record struct ExprFragment(ExprType Kind, ReadOnlyMemory<char> Text, int Position, object? Value = null);