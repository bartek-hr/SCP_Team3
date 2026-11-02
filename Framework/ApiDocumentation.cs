namespace CargoHUB.Framework;

public static class ApiDocumentation
{
    /// <summary>Registers independent contracts for the supported API major versions.</summary>
    public static IServiceCollection AddVersionedOpenApi(this IServiceCollection services)
    {
        foreach (var version in new[] { 1, 2 })
        {
            var documentName = $"v{version}";
            services.AddOpenApi(documentName, options =>
            {
                options.ShouldInclude = description => description.GroupName == documentName;
                options.AddDocumentTransformer((document, _, _) =>
                {
                    document.Info = new()
                    {
                        Title = "CargoHUB WMS",
                        Version = $"{version}.0",
                    };
                    return Task.CompletedTask;
                });
            });
        }

        return services;
    }
}
