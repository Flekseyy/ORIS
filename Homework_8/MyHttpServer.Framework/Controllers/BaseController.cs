using System.Net;
using MyHttpServer.Framework.Authentication;
using MyHttpServer.Framework.Result;

namespace MyHttpServer.Framework.Controllers;

public abstract class BaseController
{
    protected HttpListenerContext? Context{get; private set;}
    
    protected IUserSessionService SessionService
    {
        get;
        private set;
    } = null!;
    public void Initialize(
        HttpListenerContext context,
        IUserSessionService sessionService)
    {
        Context = context;
        SessionService = sessionService;
    }
    protected static IActionResult Json(object data) => new JsonResult(data);
    protected static IActionResult PageResult(string path, object model) => new PageResult(path, model);
    protected static IActionResult View(string viewName) => new ViewResult(viewName);
    protected static IHttpResponseTypeResult Html(string html) => new HtmlResult(html);
}