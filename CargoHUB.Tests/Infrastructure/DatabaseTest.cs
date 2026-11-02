using CargoHUB.Datasource;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CargoHUB.Tests.Infrastructure;

public abstract class DatabaseTest
{
    protected CargoHubDbContext Context { get; private set; } = null!;
    private SqliteConnection _connection = null!;

    [TestInitialize]
    public void InitializeDatabase()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        DbContextOptions<CargoHubDbContext> options = new DbContextOptionsBuilder<CargoHubDbContext>()
            .UseSqlite(_connection)
            .Options;
        Context = new CargoHubDbContext(options);
        Context.Database.EnsureCreated();
        SeedDatabase();
    }

    [TestCleanup]
    public void CleanupDatabase()
    {
        Context.Dispose();
        _connection.Dispose();
    }

    protected virtual void SeedDatabase()
    {
    }

    // Catalog items need an item line and group because of their foreign keys.
    protected void SeedCatalogItems(params int[] ids)
    {
        if (Context.ItemLines.Find(1) is null)
        {
            Context.ItemLines.Add(TestData.CreateItemLine(1));
        }

        if (Context.ItemGroups.Find(1) is null)
        {
            Context.ItemGroups.Add(TestData.CreateItemGroup(1));
        }

        Context.Items.AddRange(ids.Select(id => TestData.CreateCatalogItem(id)));
        Context.SaveChanges();
    }

    // The API returns these messages to the client, so the tests pin them down.
    protected static void AssertInvalid(Action action, string message)
    {
        ArgumentException exception = Assert.ThrowsException<ArgumentException>(action);
        Assert.AreEqual(message, exception.Message);
    }

    protected static void AssertConflict(Action action, string message)
    {
        InvalidOperationException exception = Assert.ThrowsException<InvalidOperationException>(action);
        Assert.AreEqual(message, exception.Message);
    }
}
