namespace CargoHUB.Framework;

public enum Hook
{
    Before = 0,   // default
    Around = 1,
    After  = 2,
}


// ROUTES
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public abstract class RouteAttribute(string verb, string template, int version = 1) : Attribute
{
    public string Verb { get; } = verb;
    public string Template { get; } = template;
    public int Version { get; } = version > 0
        ? version
        : throw new ArgumentOutOfRangeException(nameof(version), "Route versions must be greater than zero.");
}

public sealed class GetAttribute(string template, int version = 1)    : RouteAttribute("GET", template, version);
public sealed class PostAttribute(string template, int version = 1)   : RouteAttribute("POST", template, version);
public sealed class PutAttribute(string template, int version = 1)    : RouteAttribute("PUT", template, version);
public sealed class PatchAttribute(string template, int version = 1)  : RouteAttribute("PATCH", template, version);
public sealed class DeleteAttribute(string template, int version = 1) : RouteAttribute("DELETE", template, version);


// MIDDLEWARE
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public sealed class MiddlewareAttribute(string name, Hook hook = Hook.Before) : Attribute
{
    public string Name { get; } = name;
    public Hook Hook { get; } = hook;
}


// USE
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
public sealed class UseAttribute(params string[] names) : Attribute
{
    public string[] Names { get; } = names;
}

// DESCRIBE

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public sealed class DescribeAttribute(string summary) : Attribute
{
    public string Summary { get; } = summary;
    public string? Description { get; set; }
    public string? OperationId { get; set; }
    public string[] Tags { get; set; } = [];
    public int SuccessStatus { get; set; } = 200;
}
