using System.Text.Json;
using System.Text.Json.Serialization;

namespace MyHttpServer.Framework.Configuration;

public class Setting
{
    
    [JsonConstructor]
    private Setting(){}
    public Server Server { get; init;} =  new ();
    private static readonly Lazy<Setting> Instance = new(Load);
    public static Setting GetInstance() => Instance.Value;
    private static readonly object _lock = new();
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        NumberHandling = JsonNumberHandling.AllowReadingFromString,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };
    private static Setting Load()
    {
        var filePath = Path.Combine(AppContext.BaseDirectory, "settings.json");

        if (!File.Exists(filePath))
        {
            Console.WriteLine($"settings.json не найден ({filePath}), используются настройки по умолчанию.");
            return new Setting();
        }

        try
        {
            var setting = JsonSerializer.Deserialize<Setting>(File.ReadAllText(filePath),JsonOptions);
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
    public string ConnectionString { get; set; } = "Host=localhost;Port=5432;Database=KardDB;Username=postgres;Password=18041804;Pooling=true;Min Pool Size=5;Max Pool Size=100";
}