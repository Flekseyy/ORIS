using System.Net;
using System.Text;
using System.Web;
using MyHttpServer.Framework.Configuration;
using MyHttpServer.Framework.Handlers;

namespace MyHttpServer.Framework.Http;

public class HttpServer
{
    private readonly HttpListener _listener = new();
    private readonly Handler _requestHandler;
    private bool _isRunning;

    public HttpServer()
    {
        var staticDir = Path.Combine(Directory.GetCurrentDirectory(), "static");

        var staticFilesHandler = new StaticFilesHandler(staticDir);
        var controllerHandler = new ControllerHandler(staticDir);
        var notFoundHandler = new NotFoundHandler(staticDir);
        
        staticFilesHandler.Successor = controllerHandler;
        controllerHandler.Successor = notFoundHandler;

        _requestHandler = staticFilesHandler;
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
             Log($"{context.Request.HttpMethod} Запрос: {context.Request.RawUrl}");
             
             await _requestHandler.HandleRequest(context);
             
             Console.WriteLine("Запрос обработан");
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


