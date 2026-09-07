using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace CargoHUB.Framework;

public static class RouteDiscovery
{
    private const string HandlersNamespace = "CargoHUB.Handlers";
    private static readonly Regex RouteParameterPattern = new(
        @"\{(?<name>[^}:]+)(?::(?<constraint>[^}]+))?\}",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public static void MapDiscoveredRoutes(this WebApplication app, Assembly? assembly = null)
    {
        ArgumentNullException.ThrowIfNull(app);

        var handlerAssembly = assembly ?? typeof(RouteDiscovery).Assembly;
        var handlerTypes = FindHandlerTypes(handlerAssembly).ToArray();
        var middlewares = MiddlewareDiscovery.Discover(handlerTypes);
        var routes = handlerTypes
            .SelectMany(FindRouteMethods)
            .Select(method => new DiscoveredRoute(method, GetDescribeAttribute(method)))
            .ToArray();

        ValidateOperationIds(routes);

        foreach (var handlerType in handlerTypes)
        {
            foreach (var discoveredRoute in routes.Where(route => route.Method.DeclaringType == handlerType))
            {
                var method = discoveredRoute.Method;
                var routeMiddlewares = MiddlewareDiscovery.ResolveForRoute(handlerType, method, middlewares);

                foreach (var route in method.GetCustomAttributes<RouteAttribute>(inherit: true))
                {
                    var endpoint = app.MapMethods(
                        GetVersionedTemplate(route),
                        [route.Verb],
                        (Delegate)(Func<HttpContext, Task>)(context =>
                            InvokeRouteAsync(context, handlerType, method, routeMiddlewares)));

                    endpoint.AddOpenApiOperationTransformer(
                        (operation, context, cancellationToken) => ConfigureOpenApiOperationAsync(
                            operation,
                            context,
                            cancellationToken,
                            method,
                            route,
                            discoveredRoute.Describe,
                            routeMiddlewares));
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

    private static string GetVersionedTemplate(RouteAttribute route)
    {
        var template = route.Template.StartsWith("/", StringComparison.Ordinal)
            ? route.Template
            : $"/{route.Template}";

        return $"/api/v{route.Version}{template}";
    }

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

        object? routeResult;
        try
        {
            routeResult = await HandlerInvoker.InvokeAsync(context, handlerType, routeMethod);
        }
        catch (RequestBindingException)
        {
            await HandlerInvoker.ExecuteResultAsync(context, Response.BadRequest());
            return;
        }

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

    private static DescribeAttribute GetDescribeAttribute(MethodInfo method)
    {
        var descriptions = method.GetCustomAttributes<DescribeAttribute>(inherit: true).ToArray();
        if (descriptions.Length != 1)
        {
            throw new InvalidOperationException(
                $"Route {method.DeclaringType?.FullName}.{method.Name} must declare exactly one [Describe] attribute.");
        }

        if (descriptions[0].SuccessStatus is < 200 or > 299)
        {
            throw new InvalidOperationException(
                $"Route {method.DeclaringType?.FullName}.{method.Name} has invalid success status " +
                $"{descriptions[0].SuccessStatus}. SuccessStatus must be a 2xx code.");
        }

        ValidateRequestSignature(method);
        return descriptions[0];
    }

    private static void ValidateOperationIds(IEnumerable<DiscoveredRoute> routes)
    {
        var duplicate = routes
            .Where(route => !string.IsNullOrWhiteSpace(route.Describe.OperationId))
            .SelectMany(discoveredRoute => discoveredRoute.Method
                .GetCustomAttributes<RouteAttribute>(inherit: true)
                .Select(route => new DiscoveredOperation(discoveredRoute, route)))
            .GroupBy(operation => operation.Route.Describe.OperationId!, StringComparer.Ordinal)
            .FirstOrDefault(group => group.Count() > 1);

        if (duplicate is null)
            return;

        var methods = string.Join(", ", duplicate.Select(operation =>
            $"{operation.Route.Method.DeclaringType?.FullName}.{operation.Route.Method.Name}"));
        throw new InvalidOperationException(
            $"OpenAPI operation ID '{duplicate.Key}' is declared by multiple routes: {methods}.");
    }

    private static void ValidateRequestSignature(MethodInfo method)
    {
        var requestParameters = method.GetParameters()
            .Where(parameter => typeof(Request).IsAssignableFrom(parameter.ParameterType))
            .ToArray();

        if (requestParameters.Length > 1 || requestParameters.Any(parameter =>
                parameter.ParameterType != typeof(Request) &&
                (!parameter.ParameterType.IsGenericType ||
                 parameter.ParameterType.GetGenericTypeDefinition() != typeof(Request<>))))
        {
            throw new InvalidOperationException(
                $"Route {method.DeclaringType?.FullName}.{method.Name} has an unsupported Request signature. " +
                "A route may declare one Request or Request<T> parameter.");
        }
    }

    private static async Task ConfigureOpenApiOperationAsync(
        OpenApiOperation operation,
        OpenApiOperationTransformerContext context,
        CancellationToken cancellationToken,
        MethodInfo method,
        RouteAttribute route,
        DescribeAttribute describe,
        IReadOnlyList<DiscoveredMiddleware> middlewares)
    {
        operation.Summary = describe.Summary;
        operation.Description = describe.Description;
        operation.OperationId = describe.OperationId;
        var document = context.Document
                       ?? throw new InvalidOperationException("OpenAPI document was not initialized.");
        var documentTags = document.Tags
                           ?? throw new InvalidOperationException("OpenAPI document tags were not initialized.");
        foreach (var tag in describe.Tags)
        {
            if (documentTags.All(existing => !string.Equals(existing.Name, tag, StringComparison.Ordinal)))
                documentTags.Add(new OpenApiTag { Name = tag });
        }

        operation.Tags = describe.Tags
            .Select(tag => new OpenApiTagReference(tag, document, null))
            .ToHashSet();
        operation.Parameters = GetPathParameters(route.Template).Cast<IOpenApiParameter>().ToList();
        operation.Responses?.Clear();

        var requestType = GetRequestBodyType(method);
        if (requestType is not null)
        {
            var schema = await context.GetOrCreateSchemaAsync(requestType, null, cancellationToken);
            operation.RequestBody = new OpenApiRequestBody
            {
                Required = true,
                Content = new Dictionary<string, OpenApiMediaType>
                {
                    ["application/json"] = new OpenApiMediaType { Schema = schema },
                },
            };
            AddStatusResponse(operation, StatusCodes.Status400BadRequest, "Invalid JSON request body.");
        }

        var responseType = GetResponseBodyType(method);
        if (responseType is null)
        {
            AddStatusResponse(operation, describe.SuccessStatus, "Success.");
        }
        else
        {
            var schema = await context.GetOrCreateSchemaAsync(responseType, null, cancellationToken);
            AddJsonResponse(operation, describe.SuccessStatus, "Success.", schema);
        }

        if (middlewares.Any(middleware => string.Equals(middleware.Attribute.Name, "Auth", StringComparison.Ordinal)))
            AddStatusResponse(operation, StatusCodes.Status401Unauthorized, "Unauthorized.");

        AddStatusResponse(operation, StatusCodes.Status403Forbidden, "Forbidden.");
        AddStatusResponse(operation, StatusCodes.Status404NotFound, "Not found.");
        AddStatusResponse(operation, StatusCodes.Status405MethodNotAllowed, "Method not allowed.");
        AddStatusResponse(operation, StatusCodes.Status500InternalServerError, "Internal server error.");
    }

    private static Type? GetRequestBodyType(MethodInfo method) =>
        method.GetParameters()
            .Select(parameter => parameter.ParameterType)
            .FirstOrDefault(type => type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Request<>))
            ?.GetGenericArguments()[0];

    private static Type? GetResponseBodyType(MethodInfo method)
    {
        var returnType = method.ReturnType;
        if (returnType.IsGenericType &&
            (returnType.GetGenericTypeDefinition() == typeof(Task<>) ||
             returnType.GetGenericTypeDefinition() == typeof(ValueTask<>)))
        {
            returnType = returnType.GetGenericArguments()[0];
        }

        return returnType.IsGenericType && returnType.GetGenericTypeDefinition() == typeof(Response<>)
            ? returnType.GetGenericArguments()[0]
            : null;
    }

    private static IEnumerable<OpenApiParameter> GetPathParameters(string template) =>
        RouteParameterPattern.Matches(template)
            .Select(match => new OpenApiParameter
            {
                Name = match.Groups["name"].Value,
                In = ParameterLocation.Path,
                Required = true,
                Schema = CreatePathParameterSchema(match.Groups["constraint"].Value),
            });

    private static OpenApiSchema CreatePathParameterSchema(string constraint) => constraint.ToLowerInvariant() switch
    {
        "guid" => new OpenApiSchema { Type = JsonSchemaType.String, Format = "uuid" },
        "int" or "int32" => new OpenApiSchema { Type = JsonSchemaType.Integer, Format = "int32" },
        "long" or "int64" => new OpenApiSchema { Type = JsonSchemaType.Integer, Format = "int64" },
        "float" => new OpenApiSchema { Type = JsonSchemaType.Number, Format = "float" },
        "double" => new OpenApiSchema { Type = JsonSchemaType.Number, Format = "double" },
        "decimal" => new OpenApiSchema { Type = JsonSchemaType.Number, Format = "double" },
        "bool" or "boolean" => new OpenApiSchema { Type = JsonSchemaType.Boolean },
        "datetime" => new OpenApiSchema { Type = JsonSchemaType.String, Format = "date-time" },
        _ => new OpenApiSchema { Type = JsonSchemaType.String },
    };

    private static void AddStatusResponse(OpenApiOperation operation, int status, string description)
    {
        var responses = operation.Responses
                        ?? throw new InvalidOperationException("OpenAPI operation responses were not initialized.");
        responses[status.ToString(System.Globalization.CultureInfo.InvariantCulture)] =
            new OpenApiResponse { Description = description };
    }

    private static void AddJsonResponse(OpenApiOperation operation, int status, string description, OpenApiSchema schema)
    {
        var responses = operation.Responses
                        ?? throw new InvalidOperationException("OpenAPI operation responses were not initialized.");
        responses[status.ToString(System.Globalization.CultureInfo.InvariantCulture)] = new OpenApiResponse
        {
            Description = description,
            Content = new Dictionary<string, OpenApiMediaType>
            {
                ["application/json"] = new OpenApiMediaType { Schema = schema },
            },
        };
    }

    private sealed record DiscoveredRoute(MethodInfo Method, DescribeAttribute Describe);

    private sealed record DiscoveredOperation(DiscoveredRoute Route, RouteAttribute Attribute);
}
