using System.Text.Json;

namespace MyHttpServer.Framework.Configuration;

public class Setting
{
    private Setting(){}
    public Server Server { get; init;} =  new ();
    private static Setting? _instance;
    private static readonly object _lock = new();
    private class SettingDto
    {
        public Server Server { get; set; } = new();
    }   
    public static Setting Load()
    {
        string filePath = "settings.json";
        if (File.Exists(filePath))
        {
            try
            {
                string reader = File.ReadAllText(filePath);
                SettingDto? dto = JsonSerializer.Deserialize<SettingDto>(reader);
                return new Setting
                {
                    Server = new Server
                    {
                        Host = dto.Server.Host,
                        Port = dto.Server.Port,
                        Path = dto.Server.Path
                    }
                };

            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при десериализации settings.json:{ex.Message}");
            }   
        }
        return new Setting();
    }
    public static Setting GetInstance()
    {
        if (_instance == null)
        {
            lock (_lock)
            {
                if (_instance == null)
                    _instance = Load();
            }
        }
        return _instance;
    }
}


public class Server
{
    public string Host { get; set; } = "127.0.0.1";
    public string Port { get; set; } = "8888";
    public string Path { get; set; } = "/";
}