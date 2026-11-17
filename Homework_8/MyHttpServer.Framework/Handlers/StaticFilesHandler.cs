using System.Net;
using MyHttpServer.Framework.HttpServer;

namespace MyHttpServer.Framework.Handlers;

/// <summary>Handler, идущий после контроллер handler: всё, что не маршрут, пробует отдать как файл из static.</summary>
public class StaticFilesHandler(PathFinder files) : Handler
{
    public override async Task HandleRequest(HttpListenerContext context)
    {
        var path = context.Request.Url!.LocalPath.TrimEnd('/');
        if (path == "") path = "/index.html";
            
        if (await files.ServeFileAsync(context, path))
            return;

        await Successor.HandleRequest(context);
    }
}