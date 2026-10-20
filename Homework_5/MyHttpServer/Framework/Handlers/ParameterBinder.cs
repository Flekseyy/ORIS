using System.Net;
using System.Reflection;
using System.Web;

namespace MyHttpServer.Framework.Handlers;

public class ParameterBinder
{
    public object[] Bind(HttpListenerRequest request, MethodInfo method)
    {
        var parameters = method.GetParameters();
        var arguments = new object[parameters.Length];
        
        var queryParams = HttpUtility.ParseQueryString(request.Url.Query);

        var postParams = HttpUtility.ParseQueryString(string.Empty);
        if (request.HttpMethod.ToUpper() == "POST" && request.HasEntityBody)
        {
            using (var reader = new StreamReader(request.InputStream, request.ContentEncoding))
            {
                string body = reader.ReadToEnd();
                postParams = HttpUtility.ParseQueryString(body);
            }
        }
        
        for (int i = 0; i < parameters.Length; i++)
        {
            var param = parameters[i];
            string paramName = param.Name?.ToLower();
            string value = null;
            
            if (postParams[paramName] != null)
                value = postParams[paramName];
            else if (queryParams[paramName] != null)
                value = queryParams[paramName];

            if (value != null)
            {
                try
                {
                    arguments[i] = Convert.ChangeType(value, param.ParameterType);
                }
                catch
                {
                    arguments[i] = null;
                }
            }
            else
            {
                arguments[i] = param.HasDefaultValue ? param.DefaultValue : null;
            }
        }

        return arguments;
    }
}