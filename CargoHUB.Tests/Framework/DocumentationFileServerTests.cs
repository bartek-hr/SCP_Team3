using CargoHUB.Framework;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CargoHUB.Tests.Framework;

[TestClass]
public sealed class DocumentationFileServerTests
{
    [TestMethod]
    public async Task DocumentationSiteServesItsDefaultDocument()
    {
        await using DocumentationTestHost host = await DocumentationTestHost.CreateAsync();

        HttpResponseMessage redirectResponse = await host.Client.GetAsync("/docs");
        HttpResponseMessage response = await host.Client.GetAsync("/docs/");

        Assert.AreEqual(System.Net.HttpStatusCode.MovedPermanently, redirectResponse.StatusCode);
        Assert.AreEqual("/docs/", redirectResponse.Headers.Location?.AbsolutePath);
        Assert.AreEqual(System.Net.HttpStatusCode.OK, response.StatusCode);
        Assert.AreEqual("documentation", await response.Content.ReadAsStringAsync());
    }

    [TestMethod]
    public async Task ApiReferenceServesItsDefaultDocumentAndAssets()
    {
        await using DocumentationTestHost host = await DocumentationTestHost.CreateAsync();

        HttpResponseMessage indexResponse = await host.Client.GetAsync("/api/");
        HttpResponseMessage assetResponse = await host.Client.GetAsync("/api/styles/site.css");

        Assert.AreEqual(System.Net.HttpStatusCode.OK, indexResponse.StatusCode);
        Assert.AreEqual("api reference", await indexResponse.Content.ReadAsStringAsync());
        Assert.AreEqual(System.Net.HttpStatusCode.OK, assetResponse.StatusCode);
        Assert.AreEqual("text/css", assetResponse.Content.Headers.ContentType?.MediaType);
    }

    [TestMethod]
    public async Task VersionedApiRoutesBypassTheApiReferenceFileServer()
    {
        await using DocumentationTestHost host = await DocumentationTestHost.CreateAsync();

        HttpResponseMessage response = await host.Client.GetAsync("/api/v1/clients");

        Assert.AreEqual(System.Net.HttpStatusCode.OK, response.StatusCode);
        Assert.AreEqual("live api", await response.Content.ReadAsStringAsync());
    }

    private sealed class DocumentationTestHost : IAsyncDisposable
    {
        private readonly WebApplication _app;
        private readonly string _rootPath;

        private DocumentationTestHost(WebApplication app, HttpClient client, string rootPath)
        {
            _app = app;
            Client = client;
            _rootPath = rootPath;
        }

        public HttpClient Client { get; }

        public static async Task<DocumentationTestHost> CreateAsync()
        {
            string rootPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
            string documentationPath = Path.Combine(rootPath, "docs");
            string apiReferencePath = Path.Combine(rootPath, "api");
            Directory.CreateDirectory(Path.Combine(apiReferencePath, "styles"));
            Directory.CreateDirectory(documentationPath);
            await File.WriteAllTextAsync(Path.Combine(documentationPath, "index.html"), "documentation");
            await File.WriteAllTextAsync(Path.Combine(apiReferencePath, "index.html"), "api reference");
            await File.WriteAllTextAsync(Path.Combine(apiReferencePath, "styles", "site.css"), "body {}");

            WebApplicationBuilder builder = WebApplication.CreateBuilder();
            builder.WebHost.UseTestServer();
            WebApplication app = builder.Build();
            app.UseDocumentationFileServers(documentationPath, apiReferencePath);
            app.MapGet("/api/v1/clients", () => "live api");
            await app.StartAsync();

            return new DocumentationTestHost(app, app.GetTestClient(), rootPath);
        }

        public async ValueTask DisposeAsync()
        {
            Client.Dispose();
            await _app.DisposeAsync();
            Directory.Delete(_rootPath, recursive: true);
        }
    }
}
