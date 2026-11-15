using System.Net;
using MyHttpServer.Framework.HttpServer;

namespace MyHttpServer.Framework.Result;
/// <summary>Единый тип результата действия контроллера</summary>
public interface IActionResult
{
    Task ExecuteAsync(ActionContext context);
}
/// <summary>Всё, что нужно результату, чтобы записать ответ</summary>
public sealed record ActionContext(HttpListenerContext Http, PathFinder Files);

/// <summary>Класс, который записывает response </summary>
internal static class ResponseWriter
{
    public static async Task WriteAsync(HttpListenerResponse response, string contentType, byte[] data, int status = 200)
    {
        response.StatusCode = status;
        response.ContentType = contentType;
        response.ContentLength64 = data.Length;
        await using var output = response.OutputStream;
        await output.WriteAsync(data);
        response.Close();
    }  
}