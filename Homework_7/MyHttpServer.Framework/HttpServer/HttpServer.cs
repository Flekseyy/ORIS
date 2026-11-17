using System.Net;
using MyHttpServer.Framework.Authentication;
using MyHttpServer.Framework.Configuration;
using MyHttpServer.Framework.Handlers;

namespace MyHttpServer.Framework.HttpServer;

public class HttpServer
{
    private readonly HttpListener _listener = new();
    private readonly Handler _requestHandler;
    private bool _isRunning;

    public HttpServer()
    {
        var staticDir = Path.Combine(AppContext.BaseDirectory, "static");

        var files = new PathFinder(staticDir);
        IUserSessionService sessionService = new UserSessionService();
        var controllerHandler = new ControllerHandler(files,sessionService);
        var staticFilesHandler = new StaticFilesHandler(files);
        var notFoundHandler = new NotFoundHandler(files);

        controllerHandler.Successor = staticFilesHandler;
        staticFilesHandler.Successor = notFoundHandler;

        _requestHandler = controllerHandler;
    }
    private static void Log(string message)
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] {message}");
        Console.ResetColor(); 
    }
    private static void LogError(string message)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine(message);
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
            var subPath = (settings.Path ?? "").Trim().Trim('/');
            var url = $"http://{settings.Host}:{settings.Port}/{subPath}";
            
            _listener.Prefixes.Add(url);
            _listener.Start();

            Log($"Сервер запущен: {url}");

            _isRunning = true;
            _ = Task.Run(ListenLoopAsync);
        }
        catch (HttpListenerException error)
        {
            LogError($"Ошибка запуска: {error.Message}");
        }
        catch (Exception ex)
        {
            LogError($"Ошибка запуска {ex}");
        }
        
    }
     private async Task ListenLoopAsync()
     {
         while (_isRunning)
         {
             try
             {
                 var context = await _listener.GetContextAsync();
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
                 LogError($"Ошибка при ожидании запроса: {ex.Message}");
             }
         }
         Log("Цикл обработки запросов завершен.");
     }

     private async Task HandleRequestAsync(HttpListenerContext context)
     {
         try
         {
             Log($"{context.Request.HttpMethod} {context.Request.RawUrl}");
             await _requestHandler.HandleRequest(context);
         }
         catch (Exception ex)
         {
             LogError($"Ошибка обработки запроса {context.Request.RawUrl}: {ex}");
             try
             {
                 context.Response.StatusCode = 500;
             }
             catch (InvalidOperationException)
             {
             }

             try
             {
                 context.Response.Close();
             }
             catch
             {
                 // соединение уже закрыто
             }
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


