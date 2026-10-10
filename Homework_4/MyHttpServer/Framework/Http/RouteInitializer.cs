using System.Reflection;
using MyHttpServer.Framework.Attributes;
using MyHttpServer.Framework.Configuration;

namespace MyHttpServer.Framework.Http;

public static class RouteInitializer
{
    private static readonly Dictionary<string, RouteInfo> _routes;

    static RouteInitializer()
    {
        _routes = new Dictionary<string, RouteInfo>();
        BuildRoutes();
    }

    public static bool TryGetRoute(string httpMethod, string path, out RouteInfo routeInfo)
    {
        var key = $"{httpMethod} {path}".ToLower();
        return _routes.TryGetValue(key, out routeInfo);
    }

    private static void BuildRoutes()
    {
        var types = Assembly.GetExecutingAssembly().GetTypes();
        foreach (var type in types)
        {
            var controllerAttribute = type.GetCustomAttribute<ControllerAttribute>();
            if (controllerAttribute is null) continue;

            var controllerRoute = controllerAttribute.Route;
            var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
            foreach (var method in methods)
            {
                var getMethod = method.GetCustomAttribute<GetAttribute>();
                var postMethod = method.GetCustomAttribute<PostAttribute>();
                RegisterRoute(type, method, controllerRoute, getMethod, "GET");
                RegisterRoute(type, method, controllerRoute, postMethod, "POST");
            }
        }

    }

    private static void RegisterRoute(Type controllerType, MethodInfo method, string controllerRoute, Attribute? routeAttribute,
        string httpMethod)
    {
        if (routeAttribute is null) return;
        
        var routeProperty = routeAttribute.GetType().GetProperty("Route");
        var methodRoute = routeProperty?.GetValue(routeAttribute)?.ToString();
        if (string.IsNullOrEmpty(methodRoute)) return;

        var key = $"{httpMethod} /{controllerRoute}/{methodRoute}".ToLower();
        _routes.Add(key, new RouteInfo { ControllerType = controllerType, Method = method, HttpMethod = httpMethod });
    }
}