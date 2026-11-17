using System.Collections.Concurrent;
using System.Globalization;
using System.Reflection;
using Npgsql;

namespace MiniHttpServer.ORM;

public partial class OrmContext : IOrmContext
{
    private const string IdColumn = "\"id\"";
    
    private static readonly ConcurrentDictionary<Type, PropertyInfo[]> PropertyCache = new();

    private readonly string _connectionString;
    public OrmContext(string connectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);
        _connectionString = connectionString;
    }


    public async Task<IReadOnlyList<T>> GetAllAsync<T>(string tableName, CancellationToken cancellationToken = default) where T : class, new()
    {
        var sql = $"SELECT * FROM {Identifier(tableName)}";
        var result = new List<T>();

        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var map = BuildColumnMap<T>(reader);
        while (await reader.ReadAsync(cancellationToken))
        {
            result.Add(MapRowToEntity<T>(reader, map));
        }

        return result;
    }

    public async Task<T?> GetByIdAsync<T>(
        string tableName, int id,
        CancellationToken cancellationToken = default) where T : class, new()
    {
        var sql = $"SELECT * FROM {Identifier(tableName)} WHERE {IdColumn} = @id";

        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("@id", id);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken)) return null;

        return MapRowToEntity<T>(reader, BuildColumnMap<T>(reader));
    }


    /// <summary>Добавляет запись. Если у сущности есть свойство Id, в него записывается сгенерированный ключ. Возвращает 1</summary>
    public async Task<int> InsertAsync<T>(
        string tableName, T entity,
        CancellationToken cancellationToken = default) where T : class
    {
        ArgumentNullException.ThrowIfNull(entity);

        var properties = GetColumnProperties<T>();
        var table = Identifier(tableName);

        var sql = properties.Count == 0
            ? $"INSERT INTO {table} DEFAULT VALUES RETURNING {IdColumn}"
            : $"INSERT INTO {table} ({string.Join(", ", properties.Select(p => Identifier(p.Name)))}) " +
              $"VALUES ({string.Join(", ", properties.Select(p => $"@{p.Name}"))}) RETURNING {IdColumn}";

        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);

        foreach (var property in properties)
        {
            command.Parameters.AddWithValue($"@{property.Name}", property.GetValue(entity) ?? DBNull.Value);
        }

        var generated = await command.ExecuteScalarAsync(cancellationToken);
        if (generated is null) return 0;

        SetGeneratedId(entity, generated);
        return 1;
    }

    public async Task<int> UpdateAsync<T>(
        string tableName, int id, T entity,
        CancellationToken cancellationToken = default) where T : class
    {
        ArgumentNullException.ThrowIfNull(entity);

        var properties = GetColumnProperties<T>();
        if (properties.Count == 0)
            throw new InvalidOperationException($"У типа {typeof(T).Name} нет свойств для обновления.");

        var setClause = string.Join(", ", properties.Select(p => $"{Identifier(p.Name)} = @{p.Name}"));
        var sql = $"UPDATE {Identifier(tableName)} SET {setClause} WHERE {IdColumn} = @id";

        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);

        foreach (var property in properties)
        {
            command.Parameters.AddWithValue($"@{property.Name}", property.GetValue(entity) ?? DBNull.Value);
        }

        command.Parameters.AddWithValue("@id", id);

        return await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<int> DeleteAsync(
        string tableName, int id,
        CancellationToken cancellationToken = default)
    {
        var sql = $"DELETE FROM {Identifier(tableName)} WHERE {IdColumn} = @id";

        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("@id", id);

        return await command.ExecuteNonQueryAsync(cancellationToken);
    }
    /// <summary>Сопоставляет свойства типа с номерами колонок ридера. Строится один раз на запрос, а не на каждую строку</summary>
    private static List<(PropertyInfo Property, int Ordinal)> BuildColumnMap<T>(NpgsqlDataReader reader) where T : class
    {
        var columns = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < reader.FieldCount; i++)
        {
            columns[reader.GetName(i)] = i;
        }

        var map = new List<(PropertyInfo, int)>();
        foreach (var property in GetWritableProperties<T>())
        {
            if (columns.TryGetValue(property.Name, out var ordinal))
            {
                map.Add((property, ordinal));
            }
        }

        return map;
    }
    private static T MapRowToEntity<T>(NpgsqlDataReader reader, List<(PropertyInfo Property, int Ordinal)> map) where T : class, new()
    {
        var entity = new T();

        foreach (var (property, ordinal) in map)
        {
            if (reader.IsDBNull(ordinal))
            {
                if (!property.PropertyType.IsValueType || Nullable.GetUnderlyingType(property.PropertyType) != null)
                {
                    property.SetValue(entity, null);
                }

                continue;
            }

            property.SetValue(entity, ConvertValue(reader.GetValue(ordinal), property.PropertyType));
        }

        return entity;
    }

    private static object ConvertValue(object value, Type propertyType)
    {
        var targetType = Nullable.GetUnderlyingType(propertyType) ?? propertyType;

        if (targetType.IsInstanceOfType(value)) return value;
        if (targetType.IsEnum) return Enum.ToObject(targetType, value);
        if (targetType == typeof(DateOnly) && value is DateTime date) return DateOnly.FromDateTime(date);
        if (targetType == typeof(TimeOnly) && value is TimeSpan time) return TimeOnly.FromTimeSpan(time);

        return Convert.ChangeType(value, targetType, CultureInfo.InvariantCulture);
    }
    /// <summary>Записывает ключ, сгенерированный базой, в свойство Id сущности (если оно есть и доступно для записи)</summary>
    private static void SetGeneratedId<T>(T entity, object generated) where T : class
    {
        var idProperty = GetWritableProperties<T>()
            .FirstOrDefault(p => string.Equals(p.Name, "id", StringComparison.OrdinalIgnoreCase));

        idProperty?.SetValue(entity, ConvertValue(generated, idProperty.PropertyType));
    }

    /// <summary>Свойства, которые пишутся в колонки: открытые и записываемые, кроме Id</summary>
    private static List<PropertyInfo> GetColumnProperties<T>() where T : class
        => GetWritableProperties<T>()
            .Where(p => !string.Equals(p.Name, "id", StringComparison.OrdinalIgnoreCase))
            .ToList();

    private static PropertyInfo[] GetWritableProperties<T>() where T : class
        => PropertyCache.GetOrAdd(typeof(T), t =>
            t.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p is { CanWrite: true, SetMethod.IsPublic: true })
                .ToArray());

    /// <summary>Проверяет имя и возвращает его в кавычках в нижнем регистре.</summary>
    private static string Identifier(string identifier)
    {
        if (string.IsNullOrWhiteSpace(identifier) || !IdentifierRegex().IsMatch(identifier))
        {
            throw new ArgumentException("Недопустимое имя SQL-идентификатора.", nameof(identifier));
        }

        return $"\"{identifier.ToLowerInvariant()}\"";
    }

    [System.Text.RegularExpressions.GeneratedRegex(@"^[A-Za-z_][A-Za-z0-9_]*$")]
    private static partial System.Text.RegularExpressions.Regex IdentifierRegex();
}