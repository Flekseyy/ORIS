using System.Net;
using MyHttpServer.Framework.Attributes;
using MyHttpServer.Framework.Configuration;
using MyHttpServer.Framework.Controllers;
using MyHttpServer.Framework.Result;

namespace MyHttpServer.Controllers;
[Controller("auth")]
public class AuthController : BaseController
{
    [Get("login")]
    public IActionResult Get()
    {
        return View("rokstar/rokstar.html");
    }
    [Post("login")]
    public IActionResult Login(string login, string password)
    { const string testLogin = "student";
        const string testPassword = "test123";

        
        if (login != testLogin ||
            password != testPassword)
        {
            return new JsonResult(
                new
                {
                    Message = "Неверный логин или пароль"
                }
                );
        }

        var userId = 1;

        var sessionId =
            SessionService.CreateSession(userId);

        var cookie = new Cookie(
            "sessionId",
            sessionId,
            "/")
        {
            HttpOnly = true,
            Secure = Context.Request.IsSecureConnection,
            Expires = DateTime.UtcNow.AddMinutes(30)
        };

        Context.Response.SetCookie(cookie);

        return Json(new
        {
            Success = true,
            Message = "Вход выполнен"
        });
    }
    [Post("Logout")]
    public IActionResult Logout()
    {
        var sessionId =
            Context.Request.Cookies["sessionId"]?.Value;
    
        SessionService.RemoveSession(sessionId);
    
        var cookie = new Cookie(
            "sessionId",
            string.Empty,
            "/")
        {
            HttpOnly = true,
            Secure = Context.Request.IsSecureConnection,
            Expires = DateTime.UtcNow.AddDays(-1)
        };
    
        Context.Response.SetCookie(cookie);
    
        return Json(new
        {
            Success = true,
            Message = "Выход выполнен"
        });
    }
    
}