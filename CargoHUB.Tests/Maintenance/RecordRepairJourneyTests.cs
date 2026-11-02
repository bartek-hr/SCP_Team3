using System.Text.Json;
using CargoHUB.Maintenance;
using CargoHUB.Datasource;
using CargoHUB.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CargoHUB.Tests.Maintenance;

[TestClass]
public sealed class RecordRepairJourneyTests : DatabaseTest
{
    [TestMethod]
    public void PreviewApplyAndRepeatPreserveRelationshipsAndEvidence()
    {
        Context.Warehouses.Add(TestData.CreateWarehouse(1, "  Main warehouse  "));
        Context.Locations.Add(TestData.CreateLocation(1, 1));
        Context.Transfers.Add(TestData.CreateTransfer(1, 1, 1));
        Context.SaveChanges();
        Context.ChangeTracker.Clear();
        var repair = new RecordRepair(Context);

        var preview = repair.Run();
        Assert.AreEqual(1, preview.Records.Count);
        Assert.AreEqual("Recoverable", preview.Records.Single().Outcome);
        Assert.AreEqual("  Main warehouse  ", Context.Warehouses.Single().Name);
        Context.ChangeTracker.Clear();
        Assert.AreEqual(0L, Scalar("SELECT COUNT(*) FROM sqlite_master WHERE name = 'RecordRepairRuns'"));

        var applied = repair.Run(true);
        Assert.AreEqual("Repaired", applied.Records.Single().Outcome);
        Assert.AreEqual("Main warehouse", Context.Warehouses.Single().Name);
        Assert.AreEqual(1, Context.Locations.Single().WarehouseId);
        Assert.AreEqual(TestData.UpdatedAt, Context.Warehouses.Single().UpdatedAt);
        using var evidence = JsonDocument.Parse((string)Scalar("SELECT ReportJson FROM RecordRepairRuns")!);
        Assert.AreEqual("  Main warehouse  ", evidence.RootElement.GetProperty("Records")[0].GetProperty("Before").GetProperty("name").GetString());
        Assert.AreEqual(0, repair.Run(true).Records.Count);
    }

    [TestMethod]
    public void AmbiguousRecordsRemainIntactAndIdentifiableForArchival()
    {
        Context.Warehouses.Add(TestData.CreateWarehouse(1));
        Context.Locations.Add(TestData.CreateLocation(1, 1, "  Unknown warehouse  "));
        Context.ItemLines.Add(TestData.CreateItemLine(1, "   "));
        Context.ItemGroups.Add(TestData.CreateItemGroup(1));
        Context.SaveChanges();
        Context.Database.ExecuteSqlRaw("PRAGMA foreign_keys = OFF");
        Context.Database.ExecuteSqlRaw("UPDATE Locations SET warehouse_id = 999 WHERE id = 1");
        Context.Database.ExecuteSqlRaw("PRAGMA foreign_keys = ON");
        Context.Database.ExecuteSqlRaw("UPDATE ItemGroups SET created_at = 'broken-date' WHERE id = 1");
        Context.ChangeTracker.Clear();

        var report = new RecordRepair(Context).Run(true);
        Assert.AreEqual(3, report.Records.Count);
        Assert.IsTrue(report.Records.All(r => r.Outcome == "NeedsReview"));
        Assert.IsTrue(report.Records.SelectMany(r => r.Reasons).Any(r => r.Rule == "missing-related-record:Warehouses"));
        Assert.IsTrue(report.Records.SelectMany(r => r.Reasons).Any(r => r.Rule == "invalid-or-missing-date"));
        Assert.AreEqual("  Unknown warehouse  ", Context.Locations.Single().Name);
        Assert.AreEqual(999, Context.Locations.Single().WarehouseId);
        Assert.AreEqual("broken-date", Scalar("SELECT created_at FROM ItemGroups WHERE id = 1"));
        Assert.IsTrue(report.Records.All(r => r.Before.SequenceEqual(r.After)));
        Assert.AreEqual(1L, Scalar("SELECT COUNT(*) FROM RecordRepairRuns"));
    }

    [TestMethod]
    public void AuditFailureRollsBackRepairs()
    {
        Context.Warehouses.Add(TestData.CreateWarehouse(1, "  Keep original  "));
        Context.SaveChanges();
        Context.ChangeTracker.Clear();
        Context.Database.ExecuteSqlRaw("CREATE TABLE RecordRepairRuns (incompatible TEXT)");
        Assert.ThrowsException<Microsoft.Data.Sqlite.SqliteException>(() => new RecordRepair(Context).Run(true));
        Assert.AreEqual("  Keep original  ", Context.Warehouses.Single().Name);
    }

    [TestMethod]
    public void CommandUsesExistingDatabaseAndDefaultsToReadOnlyPreview()
    {
        string directory = Path.Combine(Path.GetTempPath(), "issue-74-" + Guid.NewGuid());
        Directory.CreateDirectory(directory);
        string path = Path.Combine(directory, "fixture.sqlite");
        try
        {
            Assert.AreEqual(1, RepairCommand.Execute(["--repair-records", path]));
            Assert.IsFalse(File.Exists(path));
            Assert.AreEqual(2, RepairCommand.Execute(["--repair-records", path, "--unknown"]));
            using (var fixture = new CargoHubDbContext(new DbContextOptionsBuilder<CargoHubDbContext>()
                .UseSqlite($"Data Source={path};Pooling=False").Options))
            {
                fixture.Database.EnsureCreated();
                fixture.Warehouses.Add(TestData.CreateWarehouse(1, "  CLI warehouse  "));
                fixture.SaveChanges();
            }
            Assert.AreEqual(0, RepairCommand.Execute(["--repair-records", path]));
            using (var fixture = new CargoHubDbContext(new DbContextOptionsBuilder<CargoHubDbContext>()
                .UseSqlite($"Data Source={path};Pooling=False").Options))
                Assert.AreEqual("  CLI warehouse  ", fixture.Warehouses.Single().Name);
            Assert.AreEqual(0, RepairCommand.Execute(["--repair-records", path, "--apply"]));
            using (var fixture = new CargoHubDbContext(new DbContextOptionsBuilder<CargoHubDbContext>()
                .UseSqlite($"Data Source={path};Pooling=False").Options))
                Assert.AreEqual("CLI warehouse", fixture.Warehouses.Single().Name);
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [TestMethod]
    public void NegativeStockAndCompositeIdentityRemainAvailableForReview()
    {
        Context.Database.ExecuteSqlRaw("PRAGMA foreign_keys = OFF");
        Context.Database.ExecuteSqlRaw("""
            INSERT INTO Inventories (ItemId, LocationId, QuantityOnHand, QuantityExpected,
                QuantityOrdered, QuantityAllocated, CreatedAt, UpdatedAt)
            VALUES (42, 99, -7, 0, 0, 0, '2024-01-01', '2024-01-02')
            """);
        Context.Database.ExecuteSqlRaw("PRAGMA foreign_keys = ON");
        var result = new RecordRepair(Context).Run(true).Records.Single();
        Assert.AreEqual("NeedsReview", result.Outcome);
        Assert.AreEqual(42L, result.Key["ItemId"]);
        Assert.AreEqual(99L, result.Key["LocationId"]);
        Assert.IsTrue(result.Reasons.Any(r => r.Rule == "negative-quantity-or-value"));
        Assert.AreEqual(-7L, Scalar("SELECT QuantityOnHand FROM Inventories"));
    }

    private object? Scalar(string sql)
    {
        using var command = Context.Database.GetDbConnection().CreateCommand();
        command.CommandText = sql;
        return command.ExecuteScalar();
    }
}
