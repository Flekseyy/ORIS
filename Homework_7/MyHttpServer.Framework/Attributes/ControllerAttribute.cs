namespace MyHttpServer.Framework.Attributes;
[AttributeUsage(AttributeTargets.Class)]
public class ControllerAttribute(string route) : Attribute
{
    public string Route { get;} = route;
}