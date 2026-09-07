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

        using var services = new ServiceCollection()
            .AddDbContext<CargoHubDbContext>(options => options.UseSqlite(connection))
            .BuildServiceProvider();

        await services.ApplyMigrationsAsync();

        await using var scope = services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<CargoHubDbContext>();

        Assert.AreEqual("Microsoft.EntityFrameworkCore.Sqlite", context.Database.ProviderName);
        Assert.IsTrue(await context.Database.CanConnectAsync());
    }
}
