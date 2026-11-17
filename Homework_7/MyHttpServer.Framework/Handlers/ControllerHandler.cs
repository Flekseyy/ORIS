using System.Net;
using System.Reflection;
using System.Runtime.ExceptionServices;
using MyHttpServer.Framework.Authentication;
using MyHttpServer.Framework.Configuration;
using MyHttpServer.Framework.Controllers;
using MyHttpServer.Framework.HttpServer;
using MyHttpServer.Framework.Result;

namespace MyHttpServer.Framework.Handlers;

public class ControllerHandler(PathFinder files, IUserSessionService userSessionService )  : Handler
{
    private readonly ParameterBinder _parameterBinder = new();
    
    public override async Task HandleRequest(HttpListenerContext context)
    {
        var request = context.Request;

        if (!RouteInitializer.TryGetRoute(request.HttpMethod, request.Url!.LocalPath, out var route))
        {
            await Successor.HandleRequest(context);
            return;
        }

        var controller = Activator.CreateInstance(route.ControllerType) as BaseController
                         ?? throw new InvalidOperationException($"{route.ControllerType.Name} должен наследоваться от BaseController");
        controller.Initialize(context, userSessionService);

        var arguments = _parameterBinder.Bind(request, route.Method);

        object? result;
        try
        {
            result = route.Method.Invoke(controller, arguments);
        }
        catch (TargetInvocationException ex) when (ex.InnerException is not null)
        {
            ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
            throw;
        }
        if (result is Task task)
        {
            await task;
            result = task.GetType().IsGenericType ? task.GetType().GetProperty("Result")?.GetValue(task) : null;
        }

        switch (result)
        {
            case IActionResult action:
                await action.ExecuteAsync(new ActionContext(context, files));
                break;
            case IHttpResponseTypeResult html:
                await html.ExecuteAsync(context);
                break;
            default:
                throw new InvalidOperationException(
                    $"{route.ControllerType.Name}.{route.Method.Name} должен возвращать IActionResult, а вернул {result?.GetType().Name ?? "null"}");
        }
    }
}