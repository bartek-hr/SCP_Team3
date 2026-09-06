namespace CargoHUB.Framework;

public abstract class Response : IResult
{
    private readonly List<(string Name, string Value)> _headers = [];

    public int StatusCode { get; protected set; } = StatusCodes.Status200OK;
    public IReadOnlyList<(string Name, string Value)> HeaderList => _headers;

    public Response Header(string name, string value)
    {
        _headers.Add((name, value));
        return this;
    }

    public Response WithStatus(int code)
    {
        StatusCode = code;
        return this;
    }

    protected abstract Task WriteBodyAsync(HttpContext ctx);

    public async Task ExecuteAsync(HttpContext ctx)
    {
        ctx.Response.StatusCode = StatusCode;
        foreach (var (name, value) in _headers)
            ctx.Response.Headers.Append(name, value);
        await WriteBodyAsync(ctx);
    }

    public static Response Status(int code)  => new EmptyResponse(code);
    public static Response NoContent()       => new EmptyResponse(StatusCodes.Status204NoContent);
    public static Response BadRequest()    => new EmptyResponse(StatusCodes.Status400BadRequest);
    public static Response Unauthorized()    => new EmptyResponse(StatusCodes.Status401Unauthorized);
    public static Response Forbidden()       => new EmptyResponse(StatusCodes.Status403Forbidden);
    public static Response NotFound()        => new EmptyResponse(StatusCodes.Status404NotFound);
    public static Response Conflict()        => new EmptyResponse(StatusCodes.Status409Conflict);

    public static Response<T> Ok<T>(T body)       => Response<T>.Ok(body);
    public static Response<T> Created<T>(T body)  => Response<T>.Created(body);
    public static Response<T> Accepted<T>(T body) => Response<T>.Accepted(body);

    private sealed class EmptyResponse : Response
    {
        public EmptyResponse(int status) => StatusCode = status;
        protected override Task WriteBodyAsync(HttpContext ctx) => Task.CompletedTask;
    }
}

public sealed class Response<T> : Response
{
    private readonly T? _body;

    private Response(T? body, int status)
    {
        _body = body;
        StatusCode = status;
    }

    public T? Body => _body;

    public static new Response<T> Ok(T body)       => new(body, StatusCodes.Status200OK);
    public static new Response<T> Created(T body)  => new(body, StatusCodes.Status201Created);
    public static new Response<T> Accepted(T body) => new(body, StatusCodes.Status202Accepted);
    public static new Response<T> NotFound()       => new(default, StatusCodes.Status404NotFound);

    public new Response<T> Header(string name, string value)
    {
        base.Header(name, value);
        return this;
    }

    public new Response<T> WithStatus(int code)
    {
        base.WithStatus(code);
        return this;
    }

    public Response<T> CacheFor(TimeSpan ttl) =>
        Header("Cache-Control", $"max-age={(int)ttl.TotalSeconds}");

    protected override Task WriteBodyAsync(HttpContext ctx) =>
        _body is null
            ? Task.CompletedTask
            : ctx.Response.WriteAsJsonAsync(_body, ctx.RequestAborted);
}
