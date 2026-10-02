using System.Net;

namespace MyHttpServer.Framework.Http;

public class PathFinder(string rootPath)
{
    private readonly string _rootPath =  Path.GetFullPath(rootPath);

    public async Task ServeFileAsync(HttpListenerContext context, string virtualPath)
    {
        var response = context.Response;
        
        string localPath = Path.Combine(_rootPath, virtualPath.TrimStart('/'));
        localPath = Path.GetFullPath(localPath);
        
        FileInfo fileInfo = new FileInfo(localPath);
        if (!fileInfo.Exists)
        {
            response.StatusCode = 404;
            string notFound = Path.Combine(_rootPath, "404.html");

            if (File.Exists(notFound))
            {
                TypeChecker.CheckMimeType(response, new FileInfo(notFound));
                await WriteFileAsync(response, notFound);
            }
            else
            {
                response.Close();
            }

            return;
        }
        TypeChecker.CheckMimeType(response, fileInfo);
        await WriteFileAsync(response, localPath);
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