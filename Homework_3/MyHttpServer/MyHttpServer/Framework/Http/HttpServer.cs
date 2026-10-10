using System.Net;
using System.Text;
using System.Text.Json;
using MyHttpServer.Framework.Configuration;

namespace MyHttpServer.Framework.Http;

public class HttpServer
{
    private readonly HttpListener _listener = new();
    private readonly PathFinder _fileServer;
    private bool _isRunning;
    public HttpServer()
    {
        string staticDir = Path.Combine(Directory.GetCurrentDirectory(), "static");
        _fileServer = new PathFinder(staticDir);
    }
    
    private void Log(string message)
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] {message}");
        Console.ResetColor(); 
    }
    public void Start()
    {
        if (_listener.IsListening)
        {
            Log("Сервер уже запущен.");
            return;
        }
        try
        {
            var settings = Setting.GetInstance().Server;
            var url = new Uri($"http://{settings.Host}:{settings.Port}/{settings.Path}");

            _listener.Prefixes.Add(url.ToString());
            _listener.Start();

            Log($"Сервер успешно запущен: {url}");

            _isRunning = true;
            Task.Run(ListenLoopAsync);
        }
        catch (HttpListenerException error)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Ошибка запуска: {error.Message}");
            Console.ResetColor();
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Ошибка запуска {ex}");
            Console.ResetColor(); 
        }
        
    }
     private async Task ListenLoopAsync()
     {
         while (_isRunning)
         {
             try
             {
                 HttpListenerContext context = await _listener.GetContextAsync();
                 _ = Task.Run(() => HandleRequestAsync(context));
             }
             catch (HttpListenerException)
             {
                 break;
             }
             catch (ObjectDisposedException)
             {
                 break;
             }
             catch (Exception ex)
             {
                 Console.ForegroundColor = ConsoleColor.Red;
                 Console.WriteLine($"Ошибка при ожидании запроса: {ex.Message}");
                 Console.ResetColor();
             }
         }
         Log("Цикл обработки запросов завершен.");
     }

     private async Task HandleRequestAsync(HttpListenerContext context)
     {
         try
         {
             var request = context.Request;
             string? path = request.RawUrl;
             Log($"Запрос: {request.RawUrl}");
             if (path == "/" || path == "")
             {
                 path = "/static/index.html";
             }
             else if (!path.StartsWith("/static/", StringComparison.OrdinalIgnoreCase))
             {
                 path = "/static" + path;
             }
             string virtualPath = path.Substring("/static".Length);
             await _fileServer.ServeFileAsync(context, virtualPath);
         }
         catch (Exception ex)
         {
             Console.ForegroundColor = ConsoleColor.Red;
             Console.WriteLine($"Ошибка обработки запроса: {ex.Message}");
             Console.ResetColor();
             context.Response.Close();
         }
     }
    public void Stop()
    {
        if (!_listener.IsListening)
        {
            Log("Сервер не запущен.");
            return;
        }
        Log("Остановка сервера...");
        _isRunning = false;
        _listener.Stop();
        Log("Ресурсы сервера освобождены.");
    }
}


