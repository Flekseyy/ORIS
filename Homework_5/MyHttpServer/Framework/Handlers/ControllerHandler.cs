using System.Net;
using System.Reflection;
using MyHttpServer.Framework.Configuration;
using MyHttpServer.Framework.Controllers;
using MyHttpServer.Framework.Http;
using MyHttpServer.Framework.Result;

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
                var controllerInstance = Activator.CreateInstance(routeInfo.ControllerType) as ControllerBase;
                controllerInstance?.SetContext(context);
                var arguments = _parameterBinder.Bind(request, routeInfo.Method);
                var result = routeInfo.Method.Invoke(controllerInstance, arguments);
                if (result is IHttpResponseTypeResult responseResult)
                {
                    await responseResult.ExecuteAsync(context);
                }
                else if (result is string str)
                {
                    await _resultExecutor.ExecuteAsync(context, str);
                }
                else if (result is ViewResult viewResult)
                {
                    await _resultExecutor.ExecuteAsync(context,viewResult);
                }
                else
                {
                    Console.WriteLine("Ошибка");
                    response.StatusCode = 500;
                    response.Close();
                }

            }
            catch (TargetInvocationException ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Ошибка в контроллере: {ex.InnerException?.Message}");
                Console.ResetColor();
                if (response.OutputStream.CanWrite)
                {
                    response.StatusCode = 500;
                    response.Close();
                }
            }
        }
        else
        {
            await Successor.HandleRequest(context);
        }
    }
}