using System.Net;
using MyHttpServer.Framework.Result;

namespace MyHttpServer.Framework.Controllers;

public class ControllerBase
{
    protected HttpListenerContext Context{get; private set;}
    
    public void SetContext(HttpListenerContext context)
    {
        Context = context;
    }
    protected IHttpResponseTypeResult Json(object data) => new JsonResult(data);
    protected IHttpResponseTypeResult Html(string html) => new HtmlResult(html);
}