using Microsoft.EntityFrameworkCore;

namespace CargoHUB.Datasource;

public static class DatabaseMigrationExtensions
{
    public static async Task ApplyMigrationsAsync(this IServiceProvider services)
    {
        await using var scope = services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CargoHubDbContext>();

        await dbContext.Database.MigrateAsync();
    }
}
