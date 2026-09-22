using CargoHUB.Datasource;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CargoHUB.Tests.Datasource;

[TestClass]
public sealed class DatabaseMigrationExtensionsTests
{
    [TestMethod]
    public async Task AppliesMigrationsUsingTheRegisteredCargoHubDbContextAsync()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        using ServiceProvider services = new ServiceCollection()
            .AddDbContext<CargoHubDbContext>(options => options.UseSqlite(connection))
            .BuildServiceProvider();

        await services.ApplyMigrationsAsync();

        await using AsyncServiceScope scope = services.CreateAsyncScope();
        CargoHubDbContext context = scope.ServiceProvider.GetRequiredService<CargoHubDbContext>();

        Assert.AreEqual("Microsoft.EntityFrameworkCore.Sqlite", context.Database.ProviderName);
        Assert.IsTrue(await context.Database.CanConnectAsync());
        Assert.AreEqual(300, await context.Clients.CountAsync());
        Assert.AreEqual(
            "Jumbo Amersfoort Leusderweg",
            await context.Clients
                .Where(client => client.Id == 1)
                .Select(client => client.Name)
                .SingleAsync());
    }
}
