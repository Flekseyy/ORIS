namespace MyHttpServer.MiniTemplateEngine.Expressions;

/// <summary>Базовый тип узла выражения. Position - смещение в тексте выражения.</summary>
public abstract record Expr(int Position);

/// <summary>Константа: строка, int, decimal,... и null.</summary>
public sealed record LiteralExpr(object? Value, int Position) : Expr(Position);

/// <summary>Имя: переменная цикла или свойство модели.</summary>
public sealed record IdentifierExpr(string Name, int Position) : Expr(Position);

/// <summary>Обращение к члену через точку: Target.Name.</summary>
public sealed record MemberExpr(Expr Target, string Name, int Position) : Expr(Position);

/// <summary>Индексирование: Target[Index].</summary>
public sealed record IndexExpr(Expr Target, Expr Index, int Position) : Expr(Position);

/// <summary>Унарная операция: ! или минус.</summary>
public sealed record UnaryExpr(Operation Op, Expr Operand, int Position) : Expr(Position);

/// <summary>Бинарная операция: сравнение, &amp;&amp; или ||.</summary>
public sealed record BinaryExpr(Operation Op, Expr Left, Expr Right, int Position) : Expr(Position);

/// <summary>Специальная форма $raw(expr): выводит значение без HTML-экранирования.</summary>
public sealed record RawExpr(Expr Inner, int Position) : Expr(Position);