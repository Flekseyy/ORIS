using System.Net;

namespace MyHttpServer.Framework.Result;

public class HtmlResult(string html) : IHttpResponseTypeResult
{
    public async Task ExecuteAsync(HttpListenerContext context)
    {
        context.Response.ContentType = "text/html; charset=utf-8";
        var buffer = System.Text.Encoding.UTF8.GetBytes(html);
        
        context.Response.ContentLength64 = buffer.Length;

        await using var output = context.Response.OutputStream;
        await output.WriteAsync(buffer);
        
        context.Response.Close();
    }
}
public interface IHttpResponseTypeResult
{
    Task ExecuteAsync(HttpListenerContext context);
}