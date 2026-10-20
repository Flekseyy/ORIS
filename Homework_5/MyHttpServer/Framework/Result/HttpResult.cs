using System.Net;
using System.Text.Json;

namespace MyHttpServer.Framework.Result;


public class HtmlResult(string html) : IHttpResponseTypeResult
{
    public async Task ExecuteAsync(HttpListenerContext context)
    {
        context.Response.ContentType = "text/html; charset=utf-8";
        var buffer = System.Text.Encoding.UTF8.GetBytes(html);
        
        context.Response.ContentLength64 = buffer.Length;
        
        using Stream output = context.Response.OutputStream;
        await output.WriteAsync(buffer);
        
        context.Response.Close();
    }
}