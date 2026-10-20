using System.Text.Json;
using MyHttpServer.Framework.Configuration;
using MyHttpServer.Framework.Http;

namespace MyHttpServer;

public static class Program
{
    public static void Main(string[] args)
    {
        Console.Clear();
        HttpServer server = new HttpServer();
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
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Неизвестная команда.");
                    Console.ResetColor(); 
                    break;
            }
        }
    }
}



