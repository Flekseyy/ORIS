using MyHttpServer.Framework.Attributes;
using MyHttpServer.Framework.Result;
using MyHttpServer.Framework.Services;

namespace MyHttpServer.Framework.Controllers;
[Controller("home-work")]
public class HomeworkController : ControllerBase
{
   
    [Get("send")]
    public IHttpResponseTypeResult Send(string to)
    {
        try
        {
            Console.WriteLine($"Получен запрос на отправку письма на: {to}");
            EmailService.SendEmailAsync(to).GetAwaiter().GetResult();

            return Json(new { status = "success", message = $"Письмо отправлено: {to}"});
        }
        catch (Exception e)
        {
            return Json(new { status = "error", message = e.Message });
        }
        
    }
}