using System.Net;
using System.Text;
using System.Text.Json;

namespace MyHttpServer.Framework.Result;


public class JsonResult(object data) : IActionResult
{

    public Task ExecuteAsync(ActionContext context) =>
        ResponseWriter.WriteAsync(
            context.Http.Response,
            "application/json; charset=utf-8",
            Encoding.UTF8.GetBytes(JsonSerializer.Serialize(data)));
}
