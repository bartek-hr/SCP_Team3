using Microsoft.Extensions.FileProviders;

namespace CargoHUB.Framework;

public static class DocumentationFileServer
{
    public static void UseDocumentationFileServers(
        this WebApplication app,
        string documentationPath,
        string apiReferencePath)
    {
        ArgumentNullException.ThrowIfNull(app);
        ArgumentException.ThrowIfNullOrWhiteSpace(documentationPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(apiReferencePath);

        app.UseFileServer(CreateOptions(documentationPath, "/docs"));
        app.UseWhen(
            context => !IsVersionedApiRequest(context.Request.Path),
            branch => branch.UseFileServer(CreateOptions(apiReferencePath, "/api")));
    }

    private static FileServerOptions CreateOptions(string physicalPath, string requestPath) => new()
    {
        FileProvider = new PhysicalFileProvider(physicalPath),
        RequestPath = requestPath,
        EnableDirectoryBrowsing = false,
    };

    private static bool IsVersionedApiRequest(PathString path) =>
        path.Value?.StartsWith("/api/v", StringComparison.OrdinalIgnoreCase) is true;
}
