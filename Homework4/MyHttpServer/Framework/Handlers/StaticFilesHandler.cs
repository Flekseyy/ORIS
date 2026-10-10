using System.Net;
using MyHttpServer.Framework.Http;

namespace MyHttpServer.Framework.Handlers;

public class StaticFilesHandler : Handler
{
    private readonly PathFinder _fileServer;

    public StaticFilesHandler(string staticDir)
    {
        _fileServer = new PathFinder(staticDir);
    }

    public override async Task HandleRequest(HttpListenerContext context)
    {
        var path = context.Request.Url.LocalPath;
        if (path == "/index.html" || path == "/")
        {
            await _fileServer.ServeFileAsync(context, path);
        }
        if (path.Contains("."))
        {
            await _fileServer.ServeFileAsync(context, path);
        }
        else
        {
            await Successor.HandleRequest(context);
        }
    }
}