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
}
