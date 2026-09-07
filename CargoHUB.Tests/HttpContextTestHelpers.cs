using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace CargoHUB.Tests;

internal static class HttpContextTestHelpers
{
    public static DefaultHttpContext CreateContext(IServiceProvider? services = null)
    {
        var context = new DefaultHttpContext
        {
            RequestServices = services ?? new ServiceCollection().BuildServiceProvider(),
        };

        context.Response.Body = new MemoryStream();
        return context;
    }

    public static async Task<string> ReadResponseBodyAsync(HttpContext context)
    {
        context.Response.Body.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(context.Response.Body, leaveOpen: true);
        return await reader.ReadToEndAsync();
    }
}
