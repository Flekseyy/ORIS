using MyHttpServer.Framework.Attributes;
using MyHttpServer.Framework.Configuration;
using MyHttpServer.Framework.Http;

namespace MyHttpServer.Framework.Controllers;
[Controller("auth")]
public class AuthController
{
    [Get("login")]
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
}