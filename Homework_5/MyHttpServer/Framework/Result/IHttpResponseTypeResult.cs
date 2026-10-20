using System.Net;

namespace MyHttpServer.Framework.Result;

public interface IHttpResponseTypeResult
{
    Task ExecuteAsync(HttpListenerContext context);
}