namespace MyHttpServer.Framework.Result;

public class RedirectResult(string url) : IActionResult
{
    public Task ExecuteAsync(ActionContext context)
    {
        var response = context.Http.Response;
        response.StatusCode = 302;
        response.RedirectLocation = url;
        response.Close();
        return Task.CompletedTask;
    }
}