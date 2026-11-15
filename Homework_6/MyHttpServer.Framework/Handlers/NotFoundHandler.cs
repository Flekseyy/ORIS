using System.Net;
using System.Text;
using MyHttpServer.Framework.HttpServer;

namespace MyHttpServer.Framework.Handlers;

public class NotFoundHandler(PathFinder files) : Handler
{
    public override async Task HandleRequest(HttpListenerContext context)
    {
        var response = context.Response;
        response.StatusCode = 404;

        byte[] buffer;
        if (files.TryResolve("/404.html", out var notFoundPath))
        {
            buffer = await File.ReadAllBytesAsync(notFoundPath);
            response.ContentType = "text/html; charset=utf-8";
        }
        else
        {
            buffer = Encoding.UTF8.GetBytes("404 Not Found");
            response.ContentType = "text/plain; charset=utf-8";
        }

        response.ContentLength64 = buffer.Length;
        using Stream output = response.OutputStream;
        await output.WriteAsync(buffer);
        response.Close();
    }
}
