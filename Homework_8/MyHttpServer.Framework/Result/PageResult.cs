
using System.Text;
using MyHttpServer.MiniTemplateEngine;

namespace MyHttpServer.Framework.Result;
public class PageResult(string path, object model) : IActionResult
{
    public async Task ExecuteAsync(ActionContext context)
    {
        var response = context.Http.Response;
        if (!context.Files.TryResolve(path, out var fullPath))
        {
            response.StatusCode = 404;
            var errorText = $"Шаблон не найден: {path}";
            var errorBytes = Encoding.UTF8.GetBytes(errorText);
            
            response.ContentType = "text/plain; charset=utf-8";
            response.ContentLength64 = errorBytes.Length;
            await response.OutputStream.WriteAsync(errorBytes);
            response.Close();
            return;
        }
        var htmlTemplate = await File.ReadAllTextAsync(fullPath, Encoding.UTF8);
        var renderedHtml = TemplateEngine.Render(htmlTemplate, model);
        
        var responseBytes = Encoding.UTF8.GetBytes(renderedHtml);
        await ResponseWriter.WriteAsync(response, "text/html; charset=utf-8", responseBytes);
    }
        
}
