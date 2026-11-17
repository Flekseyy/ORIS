namespace MyHttpServer.MiniTemplateEngine.Expressions;

/// <summary>
/// Источник данных для вычислителя выражений. Вычислитель не знает, откуда берутся значения.
/// </summary>
public interface IExprContext
{
    /// <summary>Модель, переданная в Render. Имена, не найденные среди переменных, ищутся в ней.</summary>
    object Model { get; }

    /// <summary>Ищет локальную переменную (например, переменную цикла).</summary>
    bool TryGetVariable(string name, out object? value);

    /// <summary>Читает свойство, поле или ключ словаря.</summary>
    object? GetMember(object target, string name);

    /// <summary>Читает элемент по индексу или ключу.</summary>
    object? GetIndex(object target, object? index);
}
