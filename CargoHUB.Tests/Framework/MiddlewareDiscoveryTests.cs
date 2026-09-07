using CargoHUB.Framework;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CargoHUB.Tests.Framework;

[TestClass]
public sealed class MiddlewareDiscoveryTests
{
    [TestMethod]
    public void DiscoversSupportedMiddlewareReturnTypes()
    {
        var discovered = MiddlewareDiscovery.Discover([typeof(ValidMiddlewareHandler)]);

        CollectionAssert.AreEquivalent(
            new[] { "response", "void", "task", "value-task", "task-response", "value-task-response" },
            discovered.Keys.ToArray());
        Assert.AreEqual(Hook.Before, discovered["response"].Attribute.Hook);
        Assert.AreEqual(Hook.After, discovered["value-task-response"].Attribute.Hook);
        Assert.AreEqual(nameof(ValidMiddlewareHandler.Response), discovered["response"].Method.Name);
    }

    [TestMethod]
    public void RejectsMiddlewareWithUnsupportedReturnType()
    {
        var exception = Assert.ThrowsException<InvalidOperationException>(
            () => MiddlewareDiscovery.Discover([typeof(InvalidMiddlewareHandler)]));

        StringAssert.Contains(exception.Message, "InvalidMiddlewareHandler.Invalid");
        StringAssert.Contains(exception.Message, "must return Response, void, Task, ValueTask");
    }

    [TestMethod]
    public void RejectsDuplicateMiddlewareNames()
    {
        var exception = Assert.ThrowsException<InvalidOperationException>(
            () => MiddlewareDiscovery.Discover(
                new[] { typeof(FirstDuplicateMiddlewareHandler), typeof(SecondDuplicateMiddlewareHandler) }));

        StringAssert.Contains(exception.Message, "Middleware name 'duplicate'");
        StringAssert.Contains(exception.Message, "FirstDuplicateMiddlewareHandler.First");
        StringAssert.Contains(exception.Message, "SecondDuplicateMiddlewareHandler.Second");
    }

    [TestMethod]
    public void ResolvesClassAndMethodMiddlewareInDeclarationOrder()
    {
        var discovered = MiddlewareDiscovery.Discover([typeof(ScopedMiddlewareHandler)]);
        var routeMethod = typeof(ScopedMiddlewareHandler).GetMethod(nameof(ScopedMiddlewareHandler.Route))!;

        var resolved = MiddlewareDiscovery.ResolveForRoute(
            typeof(ScopedMiddlewareHandler),
            routeMethod,
            discovered);

        CollectionAssert.AreEqual(
            new[] { "class-first", "class-second", "method-first", "class-first" },
            resolved.Select(middleware => middleware.Attribute.Name).ToArray());
    }

    [TestMethod]
    public void RejectsRoutesThatReferenceUnknownMiddleware()
    {
        var routeMethod = typeof(UnknownMiddlewareRouteHandler)
            .GetMethod(nameof(UnknownMiddlewareRouteHandler.Route))!;

        var exception = Assert.ThrowsException<InvalidOperationException>(
            () => MiddlewareDiscovery.ResolveForRoute(
                typeof(UnknownMiddlewareRouteHandler),
                routeMethod,
                new Dictionary<string, DiscoveredMiddleware>(StringComparer.Ordinal)));

        StringAssert.Contains(exception.Message, "references unknown middleware 'missing'");
    }

    public sealed class ValidMiddlewareHandler
    {
        [Middleware("response")]
        public Response Response() => CargoHUB.Framework.Response.Ok();

        [Middleware("void")]
        public void Void()
        {
        }

        [Middleware("task")]
        public Task Task() => System.Threading.Tasks.Task.CompletedTask;

        [Middleware("value-task")]
        public ValueTask ValueTask() => System.Threading.Tasks.ValueTask.CompletedTask;

        [Middleware("task-response")]
        public Task<Response> TaskResponse() =>
            System.Threading.Tasks.Task.FromResult<CargoHUB.Framework.Response>(
                CargoHUB.Framework.Response.Ok());

        [Middleware("value-task-response", Hook.After)]
        public ValueTask<Response> ValueTaskResponse() =>
            System.Threading.Tasks.ValueTask.FromResult<CargoHUB.Framework.Response>(
                CargoHUB.Framework.Response.Ok());
    }

    public sealed class InvalidMiddlewareHandler
    {
        [Middleware("invalid")]
        public string Invalid() => "invalid";
    }

    public sealed class FirstDuplicateMiddlewareHandler
    {
        [Middleware("duplicate")]
        public void First()
        {
        }
    }

    public sealed class SecondDuplicateMiddlewareHandler
    {
        [Middleware("duplicate")]
        public void Second()
        {
        }
    }

    [Use("class-first", "class-second")]
    public sealed class ScopedMiddlewareHandler
    {
        [Middleware("class-first")]
        public void First()
        {
        }

        [Middleware("class-second")]
        public void Second()
        {
        }

        [Middleware("method-first")]
        public void MethodFirst()
        {
        }

        [Use("method-first", "class-first")]
        public void Route()
        {
        }
    }

    [Use("missing")]
    public sealed class UnknownMiddlewareRouteHandler
    {
        public void Route()
        {
        }
    }
}
