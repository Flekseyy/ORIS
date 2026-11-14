using System.Net;
using System.Text;

namespace MyHttpServer;

public class HttpServer
{
    private readonly HttpListener _listener;
    private readonly string _url;
    private bool _isRunning;
    public HttpServer(string url)
    {
        if (url[^1] != '/') url += "/";
        _url = url;
        _listener = new HttpListener();
        _listener.Prefixes.Add(url);
    }
    private void Log(string message)
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] {message}");
        Console.ResetColor(); 
    }
    public void Start()
    {
        if (_isRunning)
        {
            Log("Сервер уже запущен.");
            return;
        }
        try
        {
            _listener.Start();
            _isRunning = true;
            Log($"Сервер успешно запущен и слушает адрес: {_url}");
            _ = Task.Run(async () =>
            {
                while (_isRunning)
                {
                    var context = await _listener.GetContextAsync();
                    var request = context.Request;
                    var response = context.Response;
                    Log($"Получен запрос: {request.HttpMethod} {request.Url.LocalPath} от {request.RemoteEndPoint}");


                    var responseText = await File.ReadAllTextAsync("index.html");

                    byte[] buffer = Encoding.UTF8.GetBytes(responseText);

                    response.ContentLength64 = buffer.Length;
                    using Stream output = response.OutputStream;

                    await output.WriteAsync(buffer);
                    await output.FlushAsync();
                    Log($"Ответ успешно отправлен. Статус: {response.StatusCode} OK");

                }
            });

        }
        catch (HttpListenerException error)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Log($"Ошибка запуска {error}");
            Console.ResetColor(); 
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Log($"Ошибка при отправки запроса {ex.Message}");
            Console.ResetColor(); 
        }
        
    }
    
    public void Stop()
    {
        if (!_isRunning)
        {
            Log("Сервер не запущен.");
            Log("Ресурсы сервера освобождены.");
            return;
        }
        _isRunning = false;
        if (_listener.IsListening)
        {
            _listener.Stop();
        }
    }
}


