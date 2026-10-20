namespace MyHttpServer.Framework.Configuration;

public interface IActionResult { }

public class ViewResult(string viewName) : IActionResult
{
    public string ViewName { get; } = viewName;
}