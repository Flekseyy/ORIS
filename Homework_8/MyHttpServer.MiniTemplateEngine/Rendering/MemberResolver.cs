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
    private static readonly ConcurrentDictionary<Type, Dictionary<string, Func<object, object?>>> Getters = new();
    /// <summary>
    /// Возвращает значение члена target.name: ключ словаря (ключи сравниваются точно),
    /// иначе открытое свойство или открытое поле (имя без учёта регистра).
    /// </summary>
    /// <exception cref="ArgumentNullException">target или name равны null.</exception>
    /// <exception cref="ExpressionException">Такого члена нет или его чтение вызвало ошибку.</exception>
    public static object? GetMember(object target, string name)
    {
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(name);

        // у словаря сначала ищется ключ; если его нет, пробуем свойство (например, Count)
        if (target is IDictionary<string, object?> typedDictionary)
        {
            if (typedDictionary.TryGetValue(name, out var hit)) return hit;
        }
        else if (target is IDictionary dictionary && dictionary.Contains(name))
        {
            return dictionary[name];
        }

        var type = target.GetType();
        var getters = Getters.GetOrAdd(type, BuildGetters);

        if (!getters.TryGetValue(name, out var getter))
        {
            throw new ExpressionException($"У типа '{type.Name}' нет открытого свойства или поля '{name}'", 0);
        }

        try
        {
            return getter(target);
        }
        catch (TargetInvocationException ex) when (ex.InnerException is not null)
        {
            throw new ExpressionException($"Ошибка при чтении '{name}': {ex.InnerException.Message}", 0);
        } }

    /// <summary>Возвращает target[index] для словаря, списка или массива.</summary>
    /// <exception cref="ExpressionException">Нет такого ключа, индекс вне границ или тип не поддерживает индексирование.</exception>
    public static object? GetIndex(object target, object? index)
    {
        ArgumentNullException.ThrowIfNull(target);

        switch (target)
        {
            case IDictionary<string, object?> typedDictionary when index is string key:
                return typedDictionary.TryGetValue(key, out var hit)
                    ? hit
                    : throw new ExpressionException($"Ключ '{key}' не найден", 0);
            case IDictionary dictionary when index is not null && dictionary.Contains(index):
                return dictionary[index];
            case IDictionary dictionary:
                throw new ExpressionException($"Ключ '{index}' не найден", 0);
            case IList list:
            {
                var position = ToIndex(index);
                if (position < 0 || position >= list.Count)
                    throw new ExpressionException($"Индекс {position} вне границ (элементов: {list.Count})", 0);
                return list[position];
            }
            default:
                throw new ExpressionException($"Тип '{target.GetType().Name}' не поддерживает индексирование", 0);
        }
    }
    private static Dictionary<string, Func<object, object?>> BuildGetters(Type type)
    {
        var result = new Dictionary<string, Func<object, object?>>(StringComparer.OrdinalIgnoreCase);
        
        foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.Instance))
        {
            result[field.Name] = field.GetValue;
        }

        foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (property.CanRead && property.GetIndexParameters().Length == 0)
            {
                result[property.Name] = property.GetValue;
            }
        }

        return result;
    }
    private static int ToIndex(object? index) => index switch
    {
        int i => i,
        long l and >= int.MinValue and <= int.MaxValue => (int)l,
        short s => s,
        byte b => b,
        decimal d when d == decimal.Truncate(d) && d is >= int.MinValue and <= int.MaxValue => (int)d,
        _ => throw new ExpressionException("Индекс должен быть целым числом", 0),
    };
}
