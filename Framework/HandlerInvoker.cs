using System.Reflection;
using System.Runtime.ExceptionServices;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace CargoHUB.Framework;

internal static class HandlerInvoker
{
    public static async Task<object?> InvokeAsync(HttpContext context, Type handlerType, MethodInfo method)
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

        return await AwaitResultAsync(result);
    }

    public static async Task ExecuteResultAsync(HttpContext context, object? result)
    {
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
            $"Cannot resolve handler parameter '{parameter.Name}' of type '{parameterType.FullName}' " +
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
                throw new InvalidOperationException($"Could not await handler result of type '{resultType.FullName}'.");

            await asTask;
            return GetTaskResult(asTask);
        }

        return result;
    }

    private static object? GetTaskResult(Task task) =>
        task.GetType().GetProperty("Result", BindingFlags.Public | BindingFlags.Instance)?.GetValue(task);
}
