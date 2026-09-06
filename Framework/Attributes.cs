namespace CargoHUB.Framework;

public enum Hook
{
    Before = 0,   // default
    Around = 1,
    After  = 2,
}


// ROUTES
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public abstract class RouteAttribute(string verb, string template) : Attribute
{
    public string Verb { get; } = verb;
    public string Template { get; } = template;
}

public sealed class GetAttribute(string template)    : RouteAttribute("GET", template);
public sealed class PostAttribute(string template)   : RouteAttribute("POST", template);
public sealed class PutAttribute(string template)    : RouteAttribute("PUT", template);
public sealed class PatchAttribute(string template)  : RouteAttribute("PATCH", template);
public sealed class DeleteAttribute(string template) : RouteAttribute("DELETE", template);


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
