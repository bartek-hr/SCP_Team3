using CargoHUB.Framework;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CargoHUB.Tests.Framework;

[TestClass]
public sealed class HandlerInvokerTests
{
    [TestMethod]
    public async Task ResolvesSpecialParametersRegisteredServicesAndDefaultValuesAsync()
    {
        using var services = new ServiceCollection()
            .AddSingleton(new InvokerDependency("registered"))
            .BuildServiceProvider();
        var context = HttpContextTestHelpers.CreateContext(services);
        context.Request.RouteValues["id"] = "42";
        var cancellationTokenSource = new CancellationTokenSource();
        context.RequestAborted = cancellationTokenSource.Token;

        var result = await InvokeAsync(
            context,
            nameof(InvokerHandler.ResolveParameters));

        Assert.AreEqual("42|True|True|True|registered|7", result);
    }

    [TestMethod]
    public async Task CreatesInstanceHandlersThroughDependencyInjectionAndInvokesStaticHandlersAsync()
    {
        using var services = new ServiceCollection()
            .AddSingleton(new InvokerDependency("constructor-value"))
            .BuildServiceProvider();
        var context = HttpContextTestHelpers.CreateContext(services);

        var instanceResult = await InvokeAsync(
            context,
            nameof(InvokerHandler.ReadConstructorDependency));
        var staticResult = await InvokeAsync(context, nameof(InvokerHandler.StaticResult));

        Assert.AreEqual("constructor-value", instanceResult);
        Assert.AreEqual("static", staticResult);
    }

    [TestMethod]
    public async Task AwaitsTaskAndValueTaskResultsIncludingNonGenericResultsAsync()
    {
        using var services = new ServiceCollection()
            .AddSingleton(new InvokerDependency("constructor-value"))
            .BuildServiceProvider();
        var context = HttpContextTestHelpers.CreateContext(services);

        Assert.AreEqual("task", await InvokeAsync(context, nameof(InvokerHandler.TaskResult)));
        Assert.AreEqual("value-task", await InvokeAsync(context, nameof(InvokerHandler.ValueTaskResult)));
        Assert.IsNull(await InvokeAsync(context, nameof(InvokerHandler.TaskWithoutResult)));
        Assert.IsNull(await InvokeAsync(context, nameof(InvokerHandler.ValueTaskWithoutResult)));
    }

    [TestMethod]
    public async Task UnwrapsHandlerExceptionsAndReportsUnresolvableParametersAsync()
    {
        using var services = new ServiceCollection()
            .AddSingleton(new InvokerDependency("constructor-value"))
            .BuildServiceProvider();
        var context = HttpContextTestHelpers.CreateContext(services);

        var handlerException = await Assert.ThrowsExceptionAsync<InvalidOperationException>(
            () => InvokeAsync(context, nameof(InvokerHandler.ThrowingResult)));
        Assert.AreEqual("handler failure", handlerException.Message);

        var parameterException = await Assert.ThrowsExceptionAsync<InvalidOperationException>(
            () => InvokeAsync(context, nameof(InvokerHandler.NeedsUnregisteredService)));
        StringAssert.Contains(parameterException.Message, "Cannot resolve handler parameter 'dependency'");
    }

    [TestMethod]
    public async Task ExecutesIResultsJsonValuesAndNullValuesAsync()
    {
        using var services = new ServiceCollection()
            .AddLogging()
            .BuildServiceProvider();

        var resultContext = HttpContextTestHelpers.CreateContext(services);
        await HandlerInvoker.ExecuteResultAsync(resultContext, Results.Text("accepted", statusCode: 202));
        Assert.AreEqual(StatusCodes.Status202Accepted, resultContext.Response.StatusCode);
        Assert.AreEqual("accepted", await HttpContextTestHelpers.ReadResponseBodyAsync(resultContext));

        var jsonContext = HttpContextTestHelpers.CreateContext(services);
        await HandlerInvoker.ExecuteResultAsync(jsonContext, new { value = "serialized" });
        Assert.AreEqual(StatusCodes.Status200OK, jsonContext.Response.StatusCode);
        Assert.AreEqual(
            "serialized",
            System.Text.Json.JsonDocument.Parse(
                await HttpContextTestHelpers.ReadResponseBodyAsync(jsonContext))
                .RootElement.GetProperty("value")
                .GetString());

        var nullContext = HttpContextTestHelpers.CreateContext(services);
        await HandlerInvoker.ExecuteResultAsync(nullContext, null);
        Assert.AreEqual(StatusCodes.Status200OK, nullContext.Response.StatusCode);
        Assert.AreEqual(string.Empty, await HttpContextTestHelpers.ReadResponseBodyAsync(nullContext));
    }

    private static Task<object?> InvokeAsync(HttpContext context, string methodName) =>
        HandlerInvoker.InvokeAsync(
            context,
            typeof(InvokerHandler),
            typeof(InvokerHandler).GetMethod(methodName)!);

    public sealed class InvokerHandler
    {
        private readonly InvokerDependency _constructorDependency;

        public InvokerHandler(InvokerDependency constructorDependency)
        {
            _constructorDependency = constructorDependency;
        }

        public string ResolveParameters(
            Request request,
            HttpContext context,
            CancellationToken cancellationToken,
            IServiceProvider serviceProvider,
            InvokerDependency dependency,
            int count = 7) =>
            string.Join(
                '|',
                request.Param("id"),
                ReferenceEquals(context, request.Context),
                cancellationToken == context.RequestAborted,
                ReferenceEquals(serviceProvider, context.RequestServices),
                dependency.Value,
                count);

        public string ReadConstructorDependency() => _constructorDependency.Value;

        public static string StaticResult() => "static";

        public Task<string> TaskResult() => Task.FromResult("task");

        public ValueTask<string> ValueTaskResult() => ValueTask.FromResult("value-task");

        public Task TaskWithoutResult() => Task.FromResult<object?>(null);

        public ValueTask ValueTaskWithoutResult() => ValueTask.CompletedTask;

        public string ThrowingResult() => throw new InvalidOperationException("handler failure");

        public string NeedsUnregisteredService(UnregisteredDependency dependency) => dependency.Value;
    }

    public sealed record InvokerDependency(string Value);

    public sealed record UnregisteredDependency(string Value);
}
