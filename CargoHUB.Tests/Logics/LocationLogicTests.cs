using CargoHUB.Access;
using CargoHUB.Logics;
using CargoHUB.Models;
using CargoHUB.Tests.Infrastructure;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CargoHUB.Tests.Logics;

[TestClass]
public sealed class LocationLogicTests : DatabaseTest
{
    private LocationLogic Logic => new(new LocationAccess(Context), new WarehouseAccess(Context), new TransferAccess(Context));

    protected override void SeedDatabase()
    {
        Context.Warehouses.AddRange(TestData.CreateWarehouse(1), TestData.CreateWarehouse(2));
        Context.Locations.AddRange(
            TestData.CreateLocation(1, warehouseId: 1),
            TestData.CreateLocation(2, warehouseId: 1),
            TestData.CreateLocation(3, warehouseId: 2));
        Context.Transfers.Add(TestData.CreateTransfer(1, fromLocationId: 1, toLocationId: 2));
        Context.SaveChanges();
    }

    [TestMethod]
    public void GetAllAndGetByIdReturnStoredLocations()
    {
        Assert.AreEqual(3, Logic.GetAll().Count);
        Assert.AreEqual(2, Logic.GetById(3)?.WarehouseId);
        Assert.IsNull(Logic.GetById(999));
        Assert.IsNull(Logic.GetById(0));
    }

    [TestMethod]
    public void GetByWarehouseIdReturnsOnlyLocationsInThatWarehouse()
    {
        CollectionAssert.AreEqual(new[] { 1, 2 }, Logic.GetByWarehouseId(1).Select(location => location.Id).ToArray());
        CollectionAssert.AreEqual(new[] { 3 }, Logic.GetByWarehouseId(2).Select(location => location.Id).ToArray());
        Assert.AreEqual(0, Logic.GetByWarehouseId(999).Count);
        Assert.AreEqual(0, Logic.GetByWarehouseId(0).Count);
    }

    [TestMethod]
    public void AddPersistsLocationAndSetsTimestamps()
    {
        Location location = TestData.CreateLocation(warehouseId: 2, name: "New Location");

        Logic.Add(location);

        Location? stored = Logic.GetById(location.Id);
        Assert.IsNotNull(stored);
        Assert.AreEqual("New Location", stored.Name);
        Assert.AreEqual(2, stored.WarehouseId);
        Assert.IsTrue(stored.CreatedAt > TestData.UpdatedAt);
        Assert.AreEqual(stored.CreatedAt, stored.UpdatedAt);
    }

    [TestMethod]
    public void AddRejectsInvalidDuplicateAndOrphanedLocations()
    {
        Location missingCode = TestData.CreateLocation();
        missingCode.Code = string.Empty;

        Assert.ThrowsException<ArgumentNullException>(() => Logic.Add(null!));
        Assert.ThrowsException<ArgumentException>(() => Logic.Add(missingCode));
        Assert.ThrowsException<ArgumentException>(() => Logic.Add(TestData.CreateLocation(warehouseId: 999)));
        Assert.ThrowsException<ArgumentException>(() => Logic.Add(TestData.CreateLocation(warehouseId: 0)));
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => Logic.Add(TestData.CreateLocation(-1)));
        Assert.ThrowsException<InvalidOperationException>(() => Logic.Add(TestData.CreateLocation(1)));
        Assert.AreEqual(3, Logic.GetAll().Count);
    }

    [TestMethod]
    public void UpdateCanMoveLocationToAnotherWarehouseAndPreservesCreationTime()
    {
        Location updated = TestData.CreateLocation(warehouseId: 2, name: "Moved Location");
        updated.CreatedAt = DateTime.UtcNow.AddYears(1);

        Assert.IsTrue(Logic.Update(1, updated));

        Location? stored = Logic.GetById(1);
        Assert.IsNotNull(stored);
        Assert.AreEqual(2, stored.WarehouseId);
        Assert.AreEqual("Moved Location", stored.Name);
        Assert.AreEqual(TestData.CreatedAt, stored.CreatedAt);
        Assert.IsTrue(stored.UpdatedAt > TestData.UpdatedAt);
    }

    [TestMethod]
    public void UpdateReturnsFalseForMissingLocationAndRejectsInvalidData()
    {
        Assert.IsFalse(Logic.Update(999, TestData.CreateLocation()));
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => Logic.Update(0, TestData.CreateLocation()));
        Assert.ThrowsException<ArgumentException>(() => Logic.Update(1, TestData.CreateLocation(warehouseId: 999)));
        Assert.AreEqual(1, Logic.GetById(1)?.WarehouseId);
    }

    [TestMethod]
    public void RemoveDeletesUnusedLocation()
    {
        Assert.IsTrue(Logic.Remove(3));
        Assert.IsNull(Logic.GetById(3));
        Assert.IsFalse(Logic.Remove(3));
    }

    [TestMethod]
    public void RemoveRefusesLocationUsedByATransfer()
    {
        Assert.ThrowsException<InvalidOperationException>(() => Logic.Remove(1));
        Assert.ThrowsException<InvalidOperationException>(() => Logic.Remove(2));
        Assert.IsNotNull(Logic.GetById(1));
        Assert.IsNotNull(Logic.GetById(2));
    }

    [TestMethod]
    public void RemoveRejectsInvalidIds()
    {
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => Logic.Remove(0));
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => Logic.Remove(-1));
    }
}
