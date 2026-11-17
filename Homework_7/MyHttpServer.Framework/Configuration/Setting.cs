using System.Text.Json;
using System.Text.Json.Serialization;

namespace MyHttpServer.Framework.Configuration;

/// <summary>
/// Настройки приложения: читаются из settings.json рядом с программой, затем строка подключения
/// может быть переопределена переменной окружения SERVER_CONNECTION_STRING
/// </summary>
public class Setting
{
    public const string ConnectionStringVariable = "SERVER_CONNECTION_STRING";

    [JsonConstructor]
    private Setting() { }

    public Server Server { get; init; } = new();

    private static readonly Lazy<Setting> Instance = new(Load);

    public static Setting GetInstance() => Instance.Value;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        NumberHandling = JsonNumberHandling.AllowReadingFromString,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    private static Setting Load()
    {
        var setting = ReadFile();

        var fromEnvironment = Environment.GetEnvironmentVariable(ConnectionStringVariable);
        if (!string.IsNullOrWhiteSpace(fromEnvironment))
        {
            setting.Server.ConnectionString = fromEnvironment;
        }

        return setting;
    }

    private static Setting ReadFile()
    {
        var filePath = Path.Combine(AppContext.BaseDirectory, "settings.json");

        if (!File.Exists(filePath))
        {
            Console.WriteLine($"settings.json не найден ({filePath}), используются настройки по умолчанию.");
            return new Setting();
        }

        try
        {
            var setting = JsonSerializer.Deserialize<Setting>(File.ReadAllText(filePath), JsonOptions);
            return setting ?? new Setting();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка при чтении settings.json: {ex.Message}. Используются настройки по умолчанию.");
            return new Setting();
        }
    }
}

public class Server
{
    public string Host { get; set; } = "127.0.0.1";
    public string Port { get; set; } = "8888";
    public string Path { get; set; } = "/";
    public string ConnectionString { get; set; } =
        "Host=localhost;Port=5432;Database=KardDB;Username=postgres";
}