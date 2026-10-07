using CargoHUB.Datasource;
using CargoHUB.Models;
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
        Assert.AreEqual(3, await context.ItemTypes.CountAsync());
        Assert.AreEqual(
            "Single",
            await context.ItemTypes
                .Where(itemType => itemType.Id == 1)
                .Select(itemType => itemType.Name)
                .SingleAsync());
        Assert.AreEqual(
            "Jumbo Amersfoort Leusderweg",
            await context.Clients
                .Where(client => client.Id == 1)
                .Select(client => client.Name)
                .SingleAsync());
    }

    [TestMethod]
    public async Task SeedsWarehousesLocationsAndTransfersFromTheLegacyDataAsync()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using ServiceProvider services = await MigrateAsync(connection);
        await using AsyncServiceScope scope = services.CreateAsyncScope();
        CargoHubDbContext context = scope.ServiceProvider.GetRequiredService<CargoHubDbContext>();

        Assert.AreEqual(6132, await context.Shipments.CountAsync());
        Assert.AreEqual(33660, await context.Set<ShipmentItem>().CountAsync());
        Shipment shipment = await context.Shipments.Include(row => row.Items).SingleAsync(row => row.Id == 1);
        Assert.AreEqual(1, shipment.OrderId);
        Assert.AreEqual(6, shipment.Items!.Single(item => item.ItemId == 82).Amount);
        Assert.AreEqual(4854, await context.Orders.CountAsync());
        Assert.AreEqual(26498, await context.Set<OrderItem>().CountAsync());
        Order order = await context.Orders.Include(row => row.Items).SingleAsync(row => row.Id == 1);
        Assert.AreEqual(21.34m, order.Items!.Single(item => item.ItemId == 82).UnitPrice);
        Assert.AreEqual(4800, await context.Inventories.CountAsync());
        Assert.AreEqual(458, (await context.Inventories.FindAsync(119, 124))!.QuantityOnHand);

        Assert.AreEqual(10, await context.Warehouses.CountAsync());
        Assert.AreEqual(400, await context.Locations.CountAsync());
        Assert.AreEqual(800, await context.Transfers.CountAsync());
        Assert.AreEqual(407, await context.Transfers.CountAsync(transfer => transfer.TransferStatus == TransferStatus.Processed));
        Assert.AreEqual(2370, await context.TransferItems.CountAsync());
        Assert.AreEqual(
            "Jumbo DC Veghel Ambient",
            await context.Warehouses.Where(warehouse => warehouse.Id == 1).Select(warehouse => warehouse.Name).SingleAsync());
        Assert.AreEqual(
            "VGH-AMB-B12-R6-B7",
            await context.Locations.Where(location => location.Id == 1).Select(location => location.Code).SingleAsync());

        Transfer transfer = await context.Transfers.Include(stored => stored.Items).SingleAsync(stored => stored.Id == 2);
        Assert.AreEqual(TransferStatus.Processed, transfer.TransferStatus);
        CollectionAssert.AreEqual(
            new[] { (154, 82), (365, 25), (272, 39) },
            transfer.Items!.OrderBy(item => item.Id).Select(item => (item.ItemId, item.Amount)).ToArray());
    }

    [TestMethod]
    public async Task MigratedSchemaRejectsRowsThatReferToMissingRecordsAsync()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using ServiceProvider services = await MigrateAsync(connection);
        await using AsyncServiceScope scope = services.CreateAsyncScope();
        CargoHubDbContext context = scope.ServiceProvider.GetRequiredService<CargoHubDbContext>();

        context.Locations.Add(new Location { WarehouseId = 999, Code = "X", Name = "X" });
        await Assert.ThrowsExceptionAsync<DbUpdateException>(() => context.SaveChangesAsync());
        context.ChangeTracker.Clear();

        context.Transfers.Add(new Transfer { Reference = "X", FromLocationId = 1, ToLocationId = 999 });
        await Assert.ThrowsExceptionAsync<DbUpdateException>(() => context.SaveChangesAsync());
        context.ChangeTracker.Clear();

        context.Warehouses.Remove(await context.Warehouses.SingleAsync(warehouse => warehouse.Id == 1));
        await Assert.ThrowsExceptionAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }

    private static async Task<ServiceProvider> MigrateAsync(SqliteConnection connection)
    {
        ServiceProvider services = new ServiceCollection()
            .AddDbContext<CargoHubDbContext>(options => options.UseSqlite(connection))
            .BuildServiceProvider();

        await services.ApplyMigrationsAsync();
        return services;
    }
}
