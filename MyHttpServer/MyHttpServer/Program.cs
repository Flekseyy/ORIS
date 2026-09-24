using System.Text.Json;

namespace MyHttpServer;

public class Program
{
    public static void Main(string[] args)
    {
        string reader = File.ReadAllText("settings.json");
        Setting setting = JsonSerializer.Deserialize<Setting>(reader);
        HttpServer server = new HttpServer($"http://{setting.Server.Host}:{setting.Server.Port}/{setting.Server.Path}");
        Console.Clear();
        bool isRunning = true;
        while (isRunning)
        {
            Console.WriteLine();
            Console.WriteLine("Доступные команды:");
            Console.WriteLine("  start  - Запустить сервер");
            Console.WriteLine("  stop   - Остановить сервер");
            Console.WriteLine("  exit   - Остановить сервер и выйти");
            Console.WriteLine();
            var input = Console.ReadLine()?.Trim().ToLower();
            if (string.IsNullOrEmpty(input)) continue;
            var parts = input.Split(' ', 2);
            var command = parts[0];
            switch (command)
            {
                case "start":
                    server.Start();
                    break;
                case "stop":
                    server.Stop();
                    break;
                case "exit":
                    Console.WriteLine("Завершение работы");
                    server.Stop();
                    isRunning = false;
                    break;
                default:
                    Console.WriteLine("Неизвестная команда.");
                    break;
                
            }
        }
    }
}


// HttpListener server = new HttpListener();
// var reader = File.ReadAllText("settings.json");
// Setting setting = JsonSerializer.Deserialize<Setting>(reader);
//
// server.Prefixes.Add($"http://{setting.Server.Host}:{setting.Server.Port}/{setting.Server.Path}");
// server.Start();
//
// var context = await server.GetContextAsync();
//  
// var response = context.Response;
//
// var responseText = File.ReadAllText("index.html");
//
// byte[] buffer = Encoding.UTF8.GetBytes(responseText);
//
// response.ContentLength64 = buffer.Length;
// using Stream output = response.OutputStream;
//
// await output.WriteAsync(buffer);
// await output.FlushAsync();
//  
// Console.WriteLine("Запрос обработан");
//  
// server.Stop();

