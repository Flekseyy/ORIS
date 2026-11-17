using System.Net;

namespace MyHttpServer.Framework.HttpServer;

public class PathFinder(string rootPath)
{
    private static readonly StringComparison PathComparison =
        OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

    private readonly string _rootPath = Path.GetFullPath(rootPath);
    // <summary>
    /// Превращает виртуальный путь в полный путь к существующему файлу внутри корня.
    /// Возвращает false, если файла нет или путь ведёт за пределы корня 
    /// </summary>
    public bool TryResolve(string virtualPath, out string fullPath)
    {
        fullPath = string.Empty;
        if (string.IsNullOrEmpty(virtualPath)) return false;

        var relative = virtualPath.TrimStart('/', '\\');
        if (relative.Length == 0) return false;

        string candidate;
        try
        {
            candidate = Path.GetFullPath(Path.Combine(_rootPath, relative));
        }
        catch (Exception e) when (e is ArgumentException or NotSupportedException or PathTooLongException)
        {
            return false;
        }
        
        var root = Path.TrimEndingDirectorySeparator(_rootPath) + Path.DirectorySeparatorChar;
        if (!candidate.StartsWith(root, PathComparison)) return false;

        if (!File.Exists(candidate)) return false;

        fullPath = candidate;
        return true;
    }
    /// <summary>Отдаёт файл с подходящим Content-Type. Возвращает false, если файл не найден </summary>
    public async Task<bool> ServeFileAsync(HttpListenerContext context, string virtualPath)
    {
        if (!TryResolve(virtualPath, out var path)) return false;

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