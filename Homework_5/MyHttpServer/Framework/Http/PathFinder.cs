using System.Net;

namespace MyHttpServer.Framework.Http;

public class PathFinder(string rootPath)
{
    private readonly string _rootPath = Path.GetFullPath(rootPath);

    public async Task ServeFileAsync(HttpListenerContext context, string virtualPath)
    {
        var response = context.Response;
        if (virtualPath == "/")
        {
            var localPath = Path.Combine(_rootPath, "index.html");
            var fileInfo = new FileInfo(localPath);
            TypeChecker.CheckMimeType(response, fileInfo);
            await WriteFileAsync(response, localPath);
            return;
        }
        var searchPath = virtualPath.TrimStart('/');
        var isFound = false;
        while (!string.IsNullOrEmpty(searchPath))
        {
            var localPath = Path.Combine(_rootPath, searchPath);
            localPath = Path.GetFullPath(localPath);
     
            if (!localPath.StartsWith(_rootPath)) break;

            if (File.Exists(localPath))
            {
                var fileInfo = new FileInfo(localPath);
                TypeChecker.CheckMimeType(response, fileInfo);
                await WriteFileAsync(response, localPath);
                isFound = true;
                break;
            }
            
            var slashIndex = searchPath.IndexOf('/');
            if (slashIndex >= 0)
            {
                searchPath = searchPath.Substring(slashIndex + 1);
            }
            else
            {
                break;
            }
        }
        if (!isFound)
        {
            response.StatusCode = 404;
            response.Close();
        }
    }

    private async Task WriteFileAsync(HttpListenerResponse response, string path)
    {
        byte[] buffer = await File.ReadAllBytesAsync(path);
        response.ContentLength64 = buffer.Length;
        using Stream output = response.OutputStream;
        await output.WriteAsync(buffer);
        await output.FlushAsync();
        response.Close();
    }
}