using System.Reflection;

namespace CargoHUB.Framework;

internal sealed record DiscoveredMiddleware(
    Type HandlerType,
    MethodInfo Method,
    MiddlewareAttribute Attribute);

internal static class MiddlewareDiscovery
{
    public static IReadOnlyDictionary<string, DiscoveredMiddleware> Discover(
        IEnumerable<Type> handlerTypes)
    {
        var middlewares = new Dictionary<string, DiscoveredMiddleware>(StringComparer.Ordinal);

        foreach (var handlerType in handlerTypes)
        {
            foreach (var method in FindMiddlewareMethods(handlerType))
            {
                var attribute = method.GetCustomAttribute<MiddlewareAttribute>(inherit: true)!;
                ValidateReturnType(method);

                var middleware = new DiscoveredMiddleware(handlerType, method, attribute);
                if (!middlewares.TryAdd(attribute.Name, middleware))
                {
                    var existing = middlewares[attribute.Name];
                    throw new InvalidOperationException(
                        $"Middleware name '{attribute.Name}' is declared by both " +
                        $"{existing.Method.DeclaringType?.FullName}.{existing.Method.Name} and " +
                        $"{method.DeclaringType?.FullName}.{method.Name}.");
                }
            }
        }

        return middlewares;
    }

    public static IReadOnlyList<DiscoveredMiddleware> ResolveForRoute(
        Type handlerType,
        MethodInfo routeMethod,
        IReadOnlyDictionary<string, DiscoveredMiddleware> middlewares)
    {
        var names = new List<string>();

        if (handlerType.GetCustomAttribute<UseAttribute>(inherit: true) is { } classUse)
            names.AddRange(classUse.Names);

        if (routeMethod.GetCustomAttribute<UseAttribute>(inherit: true) is { } methodUse)
            names.AddRange(methodUse.Names);

        var resolved = new List<DiscoveredMiddleware>(names.Count);
        foreach (var name in names)
        {
            if (!middlewares.TryGetValue(name, out var middleware))
            {
                throw new InvalidOperationException(
                    $"Route {routeMethod.DeclaringType?.FullName}.{routeMethod.Name} " +
                    $"references unknown middleware '{name}'.");
            }

            resolved.Add(middleware);
        }

        return resolved;
    }

    private static IEnumerable<MethodInfo> FindMiddlewareMethods(Type handlerType) =>
        handlerType
            .GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static)
            .Where(method => method.GetCustomAttributes<MiddlewareAttribute>(inherit: true).Any());

    private static void ValidateReturnType(MethodInfo method)
    {
        var returnType = method.ReturnType;
        if (returnType == typeof(void) || IsResponseType(returnType))
            return;

        if (returnType == typeof(Task) || returnType == typeof(ValueTask))
            return;

        if (returnType.IsGenericType)
        {
            var genericType = returnType.GetGenericTypeDefinition();
            var resultType = returnType.GetGenericArguments()[0];

            if ((genericType == typeof(Task<>) || genericType == typeof(ValueTask<>)) &&
                IsResponseType(resultType))
            {
                return;
            }
        }

        throw new InvalidOperationException(
            $"Middleware {method.DeclaringType?.FullName}.{method.Name} must return " +
            "Response, void, Task, ValueTask, or an asynchronous Response result.");
    }

    private static bool IsResponseType(Type type) =>
        typeof(Response).IsAssignableFrom(type);
}
