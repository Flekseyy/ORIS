using System.Net;

namespace MyHttpServer.Framework.HttpServer;

public class PathFinder(string rootPath)
{
    private readonly string _rootPath = Path.GetFullPath(rootPath);

    public bool TryResolve(string virtualPath, out string fullPath)
    {
        fullPath = string.Empty;
        var relative = virtualPath.TrimStart('/', '\\');
        if (relative.Length == 0)
            return false;
        string candidate;
        try
        {
            candidate = Path.GetFullPath(Path.Combine(_rootPath, relative));
        }
        catch (Exception e) when (e is ArgumentException or NotSupportedException or PathTooLongException)
        {
            return false;
        }
        if (!File.Exists(candidate))
            return false;
        fullPath = candidate;
        return true;
    }
    public async Task<bool> ServeFileAsync(HttpListenerContext context, string virtualPath)
    {
        if (!TryResolve(virtualPath, out var path))
        {
            Console.WriteLine("PathFinder: не найден " + virtualPath);
            return false;
        }
        var response = context.Response;
        TypeChecker.CheckMimeType(response, new FileInfo(path));

        var buffer = await File.ReadAllBytesAsync(path);
        response.ContentLength64 = buffer.Length;
        await using var output = response.OutputStream;
        await output.WriteAsync(buffer);
        await output.FlushAsync();
        response.Close();
        return true;
    }
    
}