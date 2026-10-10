using System.Net;
using MyHttpServer.Framework.Configuration;
using MyHttpServer.Framework.Http;

namespace MyHttpServer.Framework.Handlers;

public class ActionResultExecutor
{
    private readonly PathFinder _fileServer;

    public ActionResultExecutor(string staticDir)
    {
        _fileServer = new PathFinder(staticDir);
    }

    public async Task ExecuteAsync(HttpListenerContext context, object result)
    {
        var response = context.Response;

        if (result is ViewResult viewResult)
        {
            var virtualPath = "/" + viewResult.ViewName;
            await _fileServer.ServeFileAsync(context, virtualPath);
        }
        else if (result is string str)
        {
            response.ContentType = "text/html; charset=utf-8";
            var buffer = System.Text.Encoding.UTF8.GetBytes(str);
            response.ContentLength64 = buffer.Length;
            using Stream output = response.OutputStream;
            await output.WriteAsync(buffer);
            response.Close();
        }
        else
        {
            response.StatusCode = 500;
            response.Close();
        }
    }
}