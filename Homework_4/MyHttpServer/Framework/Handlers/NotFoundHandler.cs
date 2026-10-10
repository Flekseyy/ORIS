using System.Net;

namespace MyHttpServer.Framework.Handlers;

public class NotFoundHandler(string staticDir) : Handler
{
    public override async Task HandleRequest(HttpListenerContext context)
    {
        var response = context.Response;
        response.StatusCode = 404;

        string notFoundPath = Path.Combine(staticDir, "404.html");

        if (File.Exists(notFoundPath))
        {
            response.ContentType = "text/html; charset=utf-8";
            byte[] buffer = await File.ReadAllBytesAsync(notFoundPath);
            response.ContentLength64 = buffer.Length;
            using Stream output = response.OutputStream;
            await output.WriteAsync(buffer);
            await output.FlushAsync();
        }

        response.Close();
    }
}