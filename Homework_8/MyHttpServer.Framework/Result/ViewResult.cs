namespace MyHttpServer.Framework.Result;
/// <summary>Отдаёт файл из папки static </summary>
public class ViewResult(string viewName) : IActionResult
{
    private string ViewName { get; } = viewName;

    public async Task ExecuteAsync(ActionContext context)
    {
        if (!await context.Files.ServeFileAsync(context.Http, "/" + ViewName))
        {
            throw new FileNotFoundException($"Представление не найдено: {ViewName}");
        }
    }
}