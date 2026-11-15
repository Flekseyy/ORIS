using System.Globalization;

namespace MyHttpServer.MiniTemplateEngine.Expressions;

/// <summary>
/// Вычисляет дерево выражения,беря данные через IExprContext
/// </summary>
public static class ExprEvaluator
{
    /// <summary>Вычисляет выражение и возвращает его значение</summary>
    public static object? Evaluate(Expr expr, IExprContext context)
    {
        ArgumentNullException.ThrowIfNull(expr);
        ArgumentNullException.ThrowIfNull(context);
        return expr switch
        {
            LiteralExpr literal => literal.Value,
            IdentifierExpr identifier => EvaluateIdentifier(identifier, context),
            MemberExpr member => EvaluateMember(member, context),
            IndexExpr index => EvaluateIndex(index, context),
            UnaryExpr unary => EvaluateUnary(unary, context),
            BinaryExpr binary => EvaluateBinary(binary, context),
            RawExpr raw => Evaluate(raw.Inner, context),
            _ => throw new InvalidOperationException($"Неизвестный узел выражения: {expr.GetType().Name}"),
        };
    }
    /// <summary>
    /// Правило истинности для if: null, false, 0 и пустая строка ложны, всё остальное истинно.
    /// </summary>
    public static bool IsTruthy(object? value)
    {
        return value switch
        {
            null => false,
            bool flag => flag,
            string text => text.Length > 0,
            int n => n != 0,
            long n => n != 0,
            short n => n != 0,
            byte n => n != 0,
            sbyte n => n != 0,
            uint n => n != 0,
            ulong n => n != 0,
            ushort n => n != 0,
            float n => n != 0f,
            double n => n != 0d,
            decimal n => n != 0m,
            _ => true
        };
    }

    private static object? EvaluateIdentifier(IdentifierExpr identifier, IExprContext context)
        => context.TryGetVariable(identifier.Name, out var variable)
            ? variable
            : context.GetMember(context.Model, identifier.Name);


    private static object? EvaluateMember(MemberExpr member, IExprContext context)
    {
        var target = Evaluate(member.Target, context);
        return target is null ? throw new ExpressionException($"Нельзя получить '{member.Name}': значение слева равно null", member.Position) : context.GetMember(target, member.Name);
    }


    private static object? EvaluateIndex(IndexExpr index, IExprContext context)
    {
        var target = Evaluate(index.Target, context);
        return target is null ? throw new ExpressionException("Нельзя обратиться по индексу к значению null", index.Position) : context.GetIndex(target, Evaluate(index.Index, context));
    }


    private static object? EvaluateUnary(UnaryExpr unary, IExprContext context)
    {
        var operand = Evaluate(unary.Operand, context);
        return unary.Op switch
        {
            Operation.Not => !IsTruthy(operand),
            Operation.Minus when operand is int n => -n,
            Operation.Minus when operand is long n => -n,
            Operation.Minus when operand is decimal n => -n,
            Operation.Minus when operand is double n => -n,
            Operation.Minus when operand is float n => -n,
            Operation.Minus when operand is short n => -n,
            Operation.Minus when operand is not null && IsNumeric(operand) => -ToDecimal(operand, unary.Position),
            Operation.Minus => throw new ExpressionException($"Нельзя применить минус к значению типа {TypeName(operand)}", unary.Position),
            _ => throw new InvalidOperationException($"Неизвестная унарная операция {unary.Op}")
        };
    }

    private static object? EvaluateBinary(BinaryExpr binary, IExprContext context)
    {
        switch (binary.Op)
        {
            case Operation.And:
                return IsTruthy(Evaluate(binary.Left, context)) && IsTruthy(Evaluate(binary.Right, context));
            case Operation.Or:
                return IsTruthy(Evaluate(binary.Left, context)) || IsTruthy(Evaluate(binary.Right, context));
        }

        var left = Evaluate(binary.Left, context);
        var right = Evaluate(binary.Right, context);
        return binary.Op switch
        {
            Operation.Eq => AreEqual(left, right, binary.Position),
            Operation.Neq => !AreEqual(left, right, binary.Position),
            Operation.Lt => Compare(left, right, binary.Position) < 0,
            Operation.Gt => Compare(left, right, binary.Position) > 0,
            Operation.Le => Compare(left, right, binary.Position) <= 0,
            Operation.Ge => Compare(left, right, binary.Position) >= 0,
            _ => throw new InvalidOperationException($"Неизвестная бинарная операция {binary.Op}")
        };
    }

    private static int Compare(object? left, object? right, int position)
    {
        if (left is null || right is null)
            throw new ExpressionException($"Нельзя сравнить {TypeName(left)} и {TypeName(right)}", position);
        if (IsNumeric(left) && IsNumeric(right))
        {
            return ToDecimal(left, position).CompareTo(ToDecimal(right, position));
        }

        if (left is string leftText && right is string rightText)
        {
            return string.CompareOrdinal(leftText, rightText);
        }

        throw new ExpressionException($"Нельзя сравнить {TypeName(left)} и {TypeName(right)}", position);
    }

    private static bool AreEqual(object? left, object? right, int position)
    {
        if (left is null || right is null) return left is null && right is null;
        if (IsNumeric(left) && IsNumeric(right))
            return ToDecimal(left, position) == ToDecimal(right, position);
        return left.Equals(right);
    }

    private static bool IsNumeric(object value)
        => value is sbyte or byte or short or ushort or int or uint or long or ulong or float or double or decimal;


    private static decimal ToDecimal(object operand, int position)
    {
        try { return Convert.ToDecimal(operand, CultureInfo.InvariantCulture); }
        catch (Exception ex) when (ex is OverflowException or InvalidCastException or FormatException)
        { throw new ExpressionException($"Нельзя преобразовать {TypeName(operand)} в число", position); }
    }
    
    private static string TypeName(object? value) => value is null ? "null" : value.GetType().Name;
}
