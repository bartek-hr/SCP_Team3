using System.Globalization;
using System.Security.Claims;

namespace CargoHUB.Framework;

public sealed class Request(HttpContext ctx)
{
    public HttpContext Context { get; } = ctx;

    public HttpRequest Raw => Context.Request;
    public RouteValueDictionary Params => Context.Request.RouteValues;
    public IHeaderDictionary Headers => Context.Request.Headers;
    public IQueryCollection QueryCollection => Context.Request.Query;
    public PathString Path => Context.Request.Path;
    public string Method=> Context.Request.Method;
    public ClaimsPrincipal User => Context.User;
    public IServiceProvider Services => Context.RequestServices;
    public CancellationToken Aborted => Context.RequestAborted;

    public string Param(string name) =>
        Params.TryGetValue(name, out var v) && v is not null
            ? v.ToString()!
            : throw new KeyNotFoundException($"Route parameter '{name}' is not present on {Path}.");

    public T Param<T>(string name) where T : IParsable<T> =>
        T.Parse(Param(name), CultureInfo.InvariantCulture);

    public bool TryParam<T>(string name, out T value) where T : IParsable<T>
    {
        value = default!;
        return Params.TryGetValue(name, out var raw)
            && raw is not null
            && T.TryParse(raw.ToString(), CultureInfo.InvariantCulture, out value!);
    }

    public string? Query(string name) => QueryCollection.TryGetValue(name, out var v) ? v.ToString() : null;

    public T? Query<T>(string name, T? fallback = default) where T : IParsable<T> =>
        Query(name) is { } raw && T.TryParse(raw, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : fallback;

    public string? Header(string name) => Headers.TryGetValue(name, out var v) ? v.ToString() : null;

    public ValueTask<T?> BodyAsync<T>() => Context.Request.ReadFromJsonAsync<T>(Aborted);

    public async ValueTask<T> RequiredBodyAsync<T>() =>
        await BodyAsync<T>() ?? throw new BadHttpRequestException($"A JSON body of type {typeof(T).Name} is required.");
}
