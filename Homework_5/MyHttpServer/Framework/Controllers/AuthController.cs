using MyHttpServer.Framework.Attributes;
using MyHttpServer.Framework.Configuration;
using MyHttpServer.Framework.Http;
using MyHttpServer.Framework.Result;

namespace MyHttpServer.Framework.Controllers;
[Controller("auth")]
public class AuthController : ControllerBase
{
    [Get("logins")]
    public IActionResult LoginGet()
    {
        return new ViewResult("rokstar/rokstar.html");
    }
    [Post("login")]
    public IActionResult LoginPost(string login, string password)
    {
        Console.WriteLine($"{login} - логин, {password} - пароль");
        return new ViewResult("rokstar/rokstar.html"); 
    }

    [Get("Method")]
    public IHttpResponseTypeResult MethodGet()
    {
        return Json(new { Login = "aaa", Password = "aaaa" });
    }
    [Get("Method1")]
    public IHttpResponseTypeResult MethodGet1()
    {
        return Html("aaaaa");
    }
}