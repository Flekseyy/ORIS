using System.Reflection;
using MyHttpServer.Framework.Attributes;
using MyHttpServer.Framework.Configuration;

namespace MyHttpServer.Framework.HttpServer;

public static class RouteInitializer
{
    private static readonly Dictionary<string, RouteInfo> Routes = new();

    static RouteInitializer()
    {
        BuildRoutes();
    }

    /// <summary>Ищет маршрут по HTTP-методу и пути</summary>
    public static bool TryGetRoute(string httpMethod, string path, out RouteInfo routeInfo)
    {
        ArgumentNullException.ThrowIfNull(httpMethod);
        ArgumentNullException.ThrowIfNull(path);

        var normalized = path.Length > 1 ? path.TrimEnd('/') : path;
        var key = $"{httpMethod} {normalized}".ToLowerInvariant();
        return Routes.TryGetValue(key, out routeInfo!);
    }

    private static void BuildRoutes()
    {
        var assembly = Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();
        Type[] types;
        try { types = assembly.GetTypes(); }
        catch (ReflectionTypeLoadException ex) { types = ex.Types.Where(t => t != null).ToArray()!; }

        foreach (var type in types)
        {
            var controllerAttribute = type.GetCustomAttribute<ControllerAttribute>();
            if (controllerAttribute is null) continue;

            var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
            foreach (var method in methods)
            {
                RegisterRoute(type, method, controllerAttribute.Route,
                    method.GetCustomAttribute<GetAttribute>()?.Route, "GET");
                RegisterRoute(type, method, controllerAttribute.Route,
                    method.GetCustomAttribute<PostAttribute>()?.Route, "POST");
            }
        }
    }


    private static void RegisterRoute(Type controllerType, MethodInfo method,
        string controllerRoute, string? methodRoute, string httpMethod)
    {
        if (string.IsNullOrEmpty(methodRoute)) return;

        var key = $"{httpMethod} /{controllerRoute}/{methodRoute}".ToLowerInvariant();
        if (!Routes.TryAdd(key, new RouteInfo { ControllerType = controllerType, Method = method, HttpMethod = httpMethod }))
            throw new InvalidOperationException($"Дублирующийся маршрут: {key} ({controllerType.Name}.{method.Name})");
    }
}