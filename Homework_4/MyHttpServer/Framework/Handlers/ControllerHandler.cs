using System.Net;
using System.Reflection;
using MyHttpServer.Framework.Http;

namespace MyHttpServer.Framework.Handlers;

public class ControllerHandler(string staticDir) : Handler
{
    private readonly ParameterBinder _parameterBinder = new();
    private readonly ActionResultExecutor _resultExecutor = new(staticDir);

    public override async Task HandleRequest(HttpListenerContext context)
    {
        var request = context.Request;
        var response = context.Response;
        var httpMethod = request.HttpMethod;
        var path = request.Url.LocalPath;

        if (RouteInitializer.TryGetRoute(httpMethod, path, out var routeInfo))
        {
            try
            {
                var controllerInstance = Activator.CreateInstance(routeInfo.ControllerType);
                var arguments = _parameterBinder.Bind(request, routeInfo.Method);
                var result = routeInfo.Method.Invoke(controllerInstance, arguments);
                await _resultExecutor.ExecuteAsync(context, result);
            }
            catch (TargetInvocationException ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Ошибка в контроллере: {ex.InnerException?.Message}");
                Console.ResetColor();
                response.StatusCode = 500;
                response.Close();
            }
        }
        else
        {
            await Successor.HandleRequest(context);
        }
    }
}