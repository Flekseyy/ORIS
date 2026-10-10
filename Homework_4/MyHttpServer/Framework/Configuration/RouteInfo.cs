using System.Reflection;

namespace MyHttpServer.Framework.Configuration;

public class RouteInfo
{
    public MethodInfo Method;
    public Type ControllerType;
    public string HttpMethod;
}