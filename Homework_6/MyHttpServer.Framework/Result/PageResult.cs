using System.Net;
using System.Text;
using MyHttpServer.Framework.HttpServer;
using MyHttpServer.MiniTemplateEngine;

namespace MyHttpServer.Framework.Result;
public class PageResult(string path, object model) : IActionResult
{
    public async Task ExecuteAsync(ActionContext context)
    {
        context.Files.TryResolve(path, out var file);
        var html =  await File.ReadAllTextAsync(file);
        var page = TemplateEngine.Render(html, model);
        await ResponseWriter.WriteAsync(context.Http.Response, "text/html; charset=utf-8", Encoding.UTF8.GetBytes(page));
    }
        
}
//todo оптимизация