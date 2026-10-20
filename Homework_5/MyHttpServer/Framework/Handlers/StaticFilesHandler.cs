using System.Net;
using MyHttpServer.Framework.Http;

namespace MyHttpServer.Framework.Handlers;

public class StaticFilesHandler(string staticDir) : Handler
{
    private readonly PathFinder _fileServer = new(staticDir);

    public override async Task HandleRequest(HttpListenerContext context)
    {
        var path = context.Request.Url.LocalPath;
        if (path == "/" || path == "/index.html")
        {
            await _fileServer.ServeFileAsync(context, "/index.html");
            return;
        }
        if (path.Contains("."))
        {
            await _fileServer.ServeFileAsync(context, path);
            return;
        }
        else
        {
            await Successor.HandleRequest(context);
        }
    }
}