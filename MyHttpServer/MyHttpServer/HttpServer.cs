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
        _url = url;
        _listener = new HttpListener();
        _listener.Prefixes.Add(url);
    }
    public void Start()
    {
        if (_isRunning)
        {
            Console.WriteLine("Сервер уже запущен");
            return;
        }
        try
        {
            _listener.Start();
            _isRunning = true;
            Console.WriteLine("Запрос обработан");
            Console.WriteLine($"Можете подключитесь к серверу по адресу {_url}");
            _ = Task.Run(async () =>
            {
                while (_isRunning)
                {
                    var context = await _listener.GetContextAsync();
                    var response = context.Response;

                    var responseText = await File.ReadAllTextAsync("index.html");

                    byte[] buffer = Encoding.UTF8.GetBytes(responseText);

                    response.ContentLength64 = buffer.Length;
                    using Stream output = response.OutputStream;

                    await output.WriteAsync(buffer);
                    await output.FlushAsync();
                }
            });

        }
        catch (HttpListenerException error)
        {
            Console.WriteLine($"Ошибка запуска {error}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка при отправки запроса {ex.Message}");
        }
        
    }
    
    public void Stop()
    {
        if (!_isRunning)
        {
            Console.WriteLine("Сервер не запущен.");
            return;
        }
        _isRunning = false;
        if (_listener.IsListening)
        {
            _listener.Stop();
            _listener.Close();
        }
        Console.WriteLine("Ресурсы сервера освобождены.");
    }
}


