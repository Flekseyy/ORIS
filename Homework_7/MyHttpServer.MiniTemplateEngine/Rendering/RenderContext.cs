using MyHttpServer.MiniTemplateEngine.Expressions;

namespace MyHttpServer.MiniTemplateEngine.Rendering;

/// <summary>
/// Состояние одного рендера: модель и переменные циклов. Данные для вычислителя выражений.
/// </summary>
public sealed class RenderContext(object model) : IExprContext
{
    // стек переменных: внутренний цикл с тем же именем временно перекрывает внешний
    private readonly List<KeyValuePair<string, object?>> _variables = [];
    public object Model { get; } = model ?? throw new ArgumentNullException(nameof(model));

    /// <summary>Вводит переменную цикла на время выполнения его тела.</summary>
    public void PushVariable(string name, object? value)
    {
        _variables.Add(new KeyValuePair<string, object?>(name, value));
    }

    /// <summary>Убирает последнюю введённую переменную; внешняя переменная с тем же именем снова становится видна.</summary>
    public void PopVariable()
    {
        _variables.RemoveAt(_variables.Count - 1);
    }

    /// <summary>
    /// Ищет переменную цикла по имени среди тех, что сейчас видны.
    /// Если одно имя введено несколько раз (вложенные циклы), возвращается самая внутренняя.
    /// </summary>
    /// <param name="name">Имя переменной, например x в $foreach (var x in Items).</param>
    /// <param name="value">Значение переменной, если она найдена, иначе null.</param>
    /// <returns>True, если переменная найдена; false, если такого имени среди переменных цикла нет.</returns>
    public bool TryGetVariable(string name, out object? value)
    {
        // ищем с конца: побеждает самая внутренняя переменная
        for (var i = _variables.Count - 1; i >= 0; i--)
        {
            if (_variables[i].Key == name)
            {
                value = _variables[i].Value;
                return true;
            }
        }

        value = null;
        return false;
    }

    /// <summary>
    /// Читает член объекта по имени: ключ словаря, открытое свойство или открытое поле.
    /// Работа делегируется MemberResolver.
    /// </summary>
    /// <param name="target">Объект, из которого читается значение. Не должен быть null.</param>
    /// <param name="name">Имя свойства, поля или ключа словаря.</param>
    /// <returns>Значение члена. Может быть null, если сам член содержит null.</returns>
    /// <exception cref="ExpressionException">У объекта нет такого члена или чтение вызвало ошибку.</exception>
    public object? GetMember(object target, string name) => MemberResolver.GetMember(target, name);

    /// <summary>
    /// Читает элемент по индексу или ключу: из массива, списка или словаря.
    /// Работа делегируется MemberResolver.
    /// </summary>
    /// <param name="target">Коллекция, из которой читается элемент. Не должна быть null.</param>
    /// <param name="index">Номер элемента для списка и массива или ключ для словаря.</param>
    /// <returns>Найденный элемент.</returns>
    /// <exception cref="ExpressionException">Нет такого ключа, индекс вне границ или тип не поддерживает индексирование.</exception>
    public object? GetIndex(object target, object? index) => MemberResolver.GetIndex(target, index);
}
