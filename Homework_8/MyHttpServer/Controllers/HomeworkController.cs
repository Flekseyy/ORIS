// using MyHttpServer.Framework.Attributes;
// using MyHttpServer.Framework.Controllers;
// using MyHttpServer.Framework.Result;
// using MyHttpServer.Services;
//
// namespace MyHttpServer.Controllers;
// [Controller("home-work")]
// public class HomeworkController : BaseController
// {
//    
//     [Get("send")]
//     public IActionResult Send(string to)
//     {
//         try
//         {
//             Console.WriteLine($"Получен запрос на отправку письма на: {to}");
//             EmailService.SendEmailAsync(to).GetAwaiter().GetResult();
//
//             return Json(new { status = "success", message = $"Письмо отправлено: {to}"});
//         }
//         catch (Exception e)
//         {
//             return Json(new { status = "error", message = e.Message });
//         }
//         
//     }
// }