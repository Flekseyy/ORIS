using System.Collections;
using System.Collections.Concurrent;
using System.Reflection;
using MyHttpServer.MiniTemplateEngine.Expressions;

namespace MyHttpServer.MiniTemplateEngine.Rendering;

/// <summary>
/// Читает данные модели с помощью рефлексии: открытые свойства и поля, словари, элементы списков и массивов.
/// </summary>
public static class MemberResolver
{
    // словарь кэшированных делегатов для чтения
    private static readonly ConcurrentDictionary<Type, Dictionary<string, Func<object, object?>>> PropertyGetters = new();

    /// <summary>Возвращает значение члена target.name: ключ словаря, открытое свойство или открытое поле.</summary>
    /// <exception cref="ExpressionException">Такого члена нет.</exception>
    public static object? GetMember(object target, string name)
    {
        var type = target.GetType();

        // в словарях сначала ищем ключ; если его нет, пробуем свойство. Получаем или создаем словарь делегатов для этого типа
        var getters = PropertyGetters.GetOrAdd(type, t =>
        {
            var d = new Dictionary<string, Func<object, object?>>(StringComparer.OrdinalIgnoreCase);
            foreach (var prop in t.GetProperties(BindingFlags.Public | BindingFlags.Instance))
                if (prop.CanRead && prop.GetIndexParameters().Length == 0)
                    d[prop.Name] = obj => prop.GetValue(obj);
            foreach (var field in t.GetFields(BindingFlags.Public | BindingFlags.Instance))
                d[field.Name] = obj => field.GetValue(obj);
            return d;
        });

        if (getters.TryGetValue(name, out var getter))
        {
            try
            {
                return getter(target);
            }
            catch (TargetInvocationException ex) when (ex.InnerException is not null)
            {
                throw new ExpressionException($"Ошибка при чтении '{name}': {ex.InnerException.Message}", 0);
            }
        }
        throw new ExpressionException($"У типа '{type.Name}' нет открытого свойства или поля '{name}'", 0);
    }

    /// <summary>Возвращает target[index] для словаря, списка или массива.</summary>
    /// <exception cref="ExpressionException">Нет такого ключа, индекс вне границ или тип не поддерживает индексирование.</exception>
    public static object? GetIndex(object target, object? index)
    {
        ArgumentNullException.ThrowIfNull(target);
        if (target is IDictionary<string, object?> typedDict && index is string key)
            return typedDict.TryGetValue(key, out var hit) ? hit : throw new ExpressionException($"Ключ '{key}' не найден", 0);
        if (target is IDictionary dict)
        {
            if (index is not null && dict.Contains(index)) return dict[index];
            throw new ExpressionException($"Ключ '{index}' не найден", 0);
        }
        if (target is IList list)
        {
            var position = ToIndex(index);
            if (position < 0 || position >= list.Count)
                throw new ExpressionException($"Индекс {position} вне границ (элементов: {list.Count})", 0);
            return list[position];
        }
        throw new ExpressionException($"Тип '{target.GetType().Name}' не поддерживает индексирование", 0);
    }
    
    private static int ToIndex(object? index) => index switch
    {
        int i => i,
        long l when l is >= int.MinValue and <= int.MaxValue => (int)l,
        short s => s,
        byte b => b,
        decimal d when d == decimal.Truncate(d) && d is >= int.MinValue and <= int.MaxValue => (int)d,
        _ => throw new ExpressionException("Индекс должен быть целым числом", 0),
    };
}
