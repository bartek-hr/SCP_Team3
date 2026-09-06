using System.Reflection;
using System.Runtime.ExceptionServices;

namespace CargoHUB.Framework;

public static class RouteDiscovery
{
    private const string HandlersNamespace = "CargoHUB.Handlers";

    public static void MapDiscoveredRoutes(this WebApplication app, Assembly? assembly = null)
    {
        ArgumentNullException.ThrowIfNull(app);

        var handlerAssembly = assembly ?? typeof(RouteDiscovery).Assembly;

        foreach (var handlerType in FindHandlerTypes(handlerAssembly))
        {
            foreach (var method in FindRouteMethods(handlerType))
            {
                foreach (var route in method.GetCustomAttributes<RouteAttribute>(inherit: true))
                {
                    app.MapMethods(
                        route.Template,
                        [route.Verb],
                        context => InvokeAsync(context, handlerType, method));
                }
            }
        }
    }

    private static IEnumerable<Type> FindHandlerTypes(Assembly assembly) =>
        assembly
            .GetTypes()
            .Where(type =>
                type is { IsClass: true, IsAbstract: false } &&
                IsInHandlersNamespace(type.Namespace));

    private static bool IsInHandlersNamespace(string? namespaceName) =>
        namespaceName is not null &&
        (namespaceName.Equals(HandlersNamespace, StringComparison.Ordinal) ||
         namespaceName.StartsWith($"{HandlersNamespace}.", StringComparison.Ordinal));

    private static IEnumerable<MethodInfo> FindRouteMethods(Type handlerType) =>
        handlerType
            .GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static)
            .Where(method => method.GetCustomAttributes<RouteAttribute>(inherit: true).Any());

    private static async Task InvokeAsync(HttpContext context, Type handlerType, MethodInfo method)
    {
        var handler = method.IsStatic
            ? null
            : ActivatorUtilities.CreateInstance(context.RequestServices, handlerType);

        var arguments = method
            .GetParameters()
            .Select(parameter => ResolveParameter(parameter, context))
            .ToArray();

        object? result;

        try
        {
            result = method.Invoke(handler, arguments);
        }
        catch (TargetInvocationException exception) when (exception.InnerException is not null)
        {
            ExceptionDispatchInfo.Capture(exception.InnerException).Throw();
            throw;
        }

        result = await AwaitResultAsync(result);

        if (result is IResult endpointResult)
        {
            await endpointResult.ExecuteAsync(context);
            return;
        }

        if (result is not null)
            await Results.Json(result).ExecuteAsync(context);
    }

    private static object? ResolveParameter(ParameterInfo parameter, HttpContext context)
    {
        var parameterType = parameter.ParameterType;

        if (parameterType == typeof(Request))
            return new Request(context);

        if (parameterType == typeof(HttpContext))
            return context;

        if (parameterType == typeof(CancellationToken))
            return context.RequestAborted;

        if (parameterType == typeof(IServiceProvider))
            return context.RequestServices;

        var service = context.RequestServices.GetService(parameterType);
        if (service is not null)
            return service;

        if (parameter.HasDefaultValue)
            return parameter.DefaultValue;

        throw new InvalidOperationException(
            $"Cannot resolve route parameter '{parameter.Name}' of type '{parameterType.FullName}' " +
            $"for {parameter.Member.DeclaringType?.FullName}.{parameter.Member.Name}.");
    }

    private static async Task<object?> AwaitResultAsync(object? result)
    {
        if (result is null)
            return null;

        if (result is Task task)
        {
            await task;
            return GetTaskResult(task);
        }

        if (result is ValueTask valueTask)
        {
            await valueTask;
            return null;
        }

        var resultType = result.GetType();
        if (resultType.IsGenericType && resultType.GetGenericTypeDefinition() == typeof(ValueTask<>))
        {
            var asTask = (Task?)resultType
                .GetMethod(nameof(ValueTask<int>.AsTask), BindingFlags.Public | BindingFlags.Instance)
                ?.Invoke(result, null);

            if (asTask is null)
                throw new InvalidOperationException($"Could not await route result of type '{resultType.FullName}'.");

            await asTask;
            return GetTaskResult(asTask);
        }

        return result;
    }

    private static object? GetTaskResult(Task task) =>
        task.GetType().GetProperty("Result", BindingFlags.Public | BindingFlags.Instance)?.GetValue(task);
}
