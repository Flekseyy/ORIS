using MyHttpServer.MiniTemplateEngine.Expressions;
using MyHttpServer.MiniTemplateEngine.LanguageStructure;

namespace MyHttpServer.MiniTemplateEngine.AST;

/// <summary>Базовый тип узла дерева шаблона. Span указывает место узла в исходном тексте.</summary>
public abstract record Node(SourceSpan Span);

/// <summary>Корень дерева: весь шаблон.</summary>
public sealed record TemplateNode(IReadOnlyList<Node> Body, SourceSpan Span) : Node(Span);
/// <summary>Обычный текст, выводится как есть.</summary>
public sealed record TextNode(ReadOnlyMemory<char> Text, SourceSpan Span) : Node(Span);

/// <summary>Вывод значения выражения: $Name, $Order.Total, $Items[0].</summary>
public sealed record OutputNode(Expr Expression, SourceSpan Span) : Node(Span);

/// <summary>Одна ветка условия: if или else if. Span указывает на заголовок ветки.</summary>
public sealed record IfBranch(Expr Condition, IReadOnlyList<Node> Body, SourceSpan Span);

/// <summary>Условие: ветки if / else if и необязательный else.</summary>
public sealed record IfNode(IReadOnlyList<IfBranch> Branches, IReadOnlyList<Node>? ElseBody, SourceSpan Span) : Node(Span);

/// <summary>Цикл: $foreach (var имя in выражение) { }.</summary>
public sealed record ForeachNode(string VariableName, Expr Collection, IReadOnlyList<Node> Body, SourceSpan Span) : Node(Span);
