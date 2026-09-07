using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Text.Json;

namespace CargoHUB.Framework;

internal static class HandlerInvoker
{
    public static async Task<object?> InvokeAsync(HttpContext context, Type handlerType, MethodInfo method)
    {
        var handler = method.IsStatic
            ? null
            : ActivatorUtilities.CreateInstance(context.RequestServices, handlerType);

        var arguments = await Task.WhenAll(method
            .GetParameters()
            .Select(parameter => ResolveParameterAsync(parameter, context)));

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

    private static async Task<object?> ResolveParameterAsync(ParameterInfo parameter, HttpContext context)
    {
        var parameterType = parameter.ParameterType;

        if (parameterType == typeof(Request))
            return new Request(context);

        if (IsTypedRequest(parameterType))
            return await BindTypedRequestAsync(context, parameterType.GetGenericArguments()[0]);

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

    private static bool IsTypedRequest(Type type) =>
        type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Request<>);

    private static async Task<object> BindTypedRequestAsync(HttpContext context, Type dataType)
    {
        try
        {
            var data = await context.Request.ReadFromJsonAsync(dataType, context.RequestAborted);
            if (data is null)
            {
                throw new RequestBindingException(
                    $"A JSON body of type {dataType.Name} is required.");
            }

            return Activator.CreateInstance(typeof(Request<>).MakeGenericType(dataType), context, data)
                   ?? throw new InvalidOperationException($"Could not create Request<{dataType.Name}>.");
        }
        catch (RequestBindingException)
        {
            throw;
        }
        catch (JsonException exception)
        {
            throw new RequestBindingException($"The JSON request body is invalid for {dataType.Name}.", exception);
        }
        catch (BadHttpRequestException exception)
        {
            throw new RequestBindingException($"The JSON request body is invalid for {dataType.Name}.", exception);
        }
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

internal sealed class RequestBindingException(string message, Exception? innerException = null)
    : Exception(message, innerException);
