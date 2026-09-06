using System.Reflection;

namespace CargoHUB.Framework;

public static class RouteDiscovery
{
    private const string HandlersNamespace = "CargoHUB.Handlers";

    public static void MapDiscoveredRoutes(this WebApplication app, Assembly? assembly = null)
    {
        ArgumentNullException.ThrowIfNull(app);

        var handlerAssembly = assembly ?? typeof(RouteDiscovery).Assembly;
        var handlerTypes = FindHandlerTypes(handlerAssembly).ToArray();
        var middlewares = MiddlewareDiscovery.Discover(handlerTypes);

        foreach (var handlerType in handlerTypes)
        {
            foreach (var method in FindRouteMethods(handlerType))
            {
                var routeMiddlewares = MiddlewareDiscovery.ResolveForRoute(handlerType, method, middlewares);

                foreach (var route in method.GetCustomAttributes<RouteAttribute>(inherit: true))
                {
                    app.MapMethods(
                        route.Template,
                        [route.Verb],
                        context => InvokeRouteAsync(context, handlerType, method, routeMiddlewares));
                }
            }
        }
    }

    private static IEnumerable<Type> FindHandlerTypes(Assembly assembly) =>
        assembly
            .GetTypes()
            .Where(type =>
                type.IsClass &&
                (!type.IsAbstract || type.IsSealed) &&
                IsInHandlersNamespace(type.Namespace));

    private static bool IsInHandlersNamespace(string? namespaceName) =>
        namespaceName is not null &&
        (namespaceName.Equals(HandlersNamespace, StringComparison.Ordinal) ||
         namespaceName.StartsWith($"{HandlersNamespace}.", StringComparison.Ordinal));

    private static IEnumerable<MethodInfo> FindRouteMethods(Type handlerType) =>
        handlerType
            .GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static)
            .Where(method => method.GetCustomAttributes<RouteAttribute>(inherit: true).Any());

    private static async Task InvokeRouteAsync(
        HttpContext context,
        Type handlerType,
        MethodInfo routeMethod,
        IReadOnlyList<DiscoveredMiddleware> middlewares)
    {
        foreach (var middleware in middlewares.Where(middleware => middleware.Attribute.Hook != Hook.After))
        {
            if (await InvokeMiddlewareAsync(context, middleware))
                return;
        }

        var routeResult = await HandlerInvoker.InvokeAsync(context, handlerType, routeMethod);

        foreach (var middleware in middlewares.Where(middleware => middleware.Attribute.Hook == Hook.After))
        {
            if (await InvokeMiddlewareAsync(context, middleware))
                return;
        }

        await HandlerInvoker.ExecuteResultAsync(context, routeResult);
    }

    private static async Task<bool> InvokeMiddlewareAsync(
        HttpContext context,
        DiscoveredMiddleware middleware)
    {
        var result = await HandlerInvoker.InvokeAsync(context, middleware.HandlerType, middleware.Method);
        if (result is null)
            return false;

        if (result is not Response response)
        {
            throw new InvalidOperationException(
                $"Middleware {middleware.Method.DeclaringType?.FullName}.{middleware.Method.Name} " +
                "returned a non-null value that is not a Response.");
        }

        await HandlerInvoker.ExecuteResultAsync(context, response);
        return true;
    }
}
