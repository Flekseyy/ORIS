using System.Net;
using System.Text.Json;
using MyHttpServer.Framework.Configuration;

namespace MyHttpServer.Framework.Result;

public class JsonResult(object data) : IHttpResponseTypeResult
{
    public async Task ExecuteAsync(HttpListenerContext context)
    {
        context.Response.ContentType = "application/json; charset=utf-8";
        string json = JsonSerializer.Serialize(data);
        var buffer = System.Text.Encoding.UTF8.GetBytes(json);
        
        context.Response.ContentLength64 = buffer.Length;
        
        using Stream output = context.Response.OutputStream;
        await output.WriteAsync(buffer);
        
        context.Response.Close();
        
    }
}