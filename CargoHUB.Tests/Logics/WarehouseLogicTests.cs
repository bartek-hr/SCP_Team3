using CargoHUB.Access;
using CargoHUB.Logics;
using CargoHUB.Models;
using CargoHUB.Tests.Infrastructure;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CargoHUB.Tests.Logics;

[TestClass]
public sealed class WarehouseLogicTests : DatabaseTest
{
    private WarehouseLogic Logic => new(new WarehouseAccess(Context), new LocationAccess(Context), new OrderDataAccess(Context));

    protected override void SeedDatabase()
    {
        Context.Warehouses.AddRange(TestData.CreateWarehouse(1), TestData.CreateWarehouse(2));
        Context.Locations.Add(TestData.CreateLocation(1, warehouseId: 2));
        Context.SaveChanges();
    }

    [TestMethod]
    public void GetAllAndGetByIdReturnStoredWarehouses()
    {
        IReadOnlyList<Warehouse> warehouses = Logic.GetAll();

        CollectionAssert.AreEqual(new[] { 1, 2 }, warehouses.Select(warehouse => warehouse.Id).ToArray());
        Assert.AreEqual("Test Warehouse", Logic.GetById(1)?.Name);
        Assert.IsNull(Logic.GetById(999));
        Assert.IsNull(Logic.GetById(0));
        Assert.IsNull(Logic.GetById(-1));
    }

    [TestMethod]
    public void AddPersistsWarehouseAndSetsTimestamps()
    {
        Warehouse warehouse = TestData.CreateWarehouse(name: "New Warehouse");

        Logic.Add(warehouse);

        Warehouse? stored = Logic.GetById(warehouse.Id);
        Assert.IsNotNull(stored);
        Assert.AreEqual("New Warehouse", stored.Name);
        Assert.IsTrue(stored.CreatedAt > TestData.UpdatedAt);
        Assert.AreEqual(stored.CreatedAt, stored.UpdatedAt);
    }

    [TestMethod]
    public void AddKeepsTheIdSentByTheClient()
    {
        Logic.Add(TestData.CreateWarehouse(9001));

        Assert.IsNotNull(Logic.GetById(9001));
    }

    [TestMethod]
    public void AddRejectsInvalidAndDuplicateWarehouses()
    {
        Warehouse missingCity = TestData.CreateWarehouse();
        missingCity.City = " ";
        Warehouse invalidEmail = TestData.CreateWarehouse();
        invalidEmail.ContactEmail = "not-an-email";

        Assert.ThrowsException<ArgumentNullException>(() => Logic.Add(null!));
        AssertInvalid(() => Logic.Add(missingCity), "city is required.");
        AssertInvalid(() => Logic.Add(invalidEmail), "contact_email must be a valid email address.");
        AssertInvalid(() => Logic.Add(TestData.CreateWarehouse(-1)), "id cannot be negative.");
        Assert.ThrowsException<InvalidOperationException>(() => Logic.Add(TestData.CreateWarehouse(1)));
        Assert.AreEqual(2, Logic.GetAll().Count);
    }

    [TestMethod]
    public void UpdatePersistsChangesAndPreservesCreationTime()
    {
        Warehouse updated = TestData.CreateWarehouse(name: "Updated Warehouse");
        updated.CreatedAt = DateTime.UtcNow.AddYears(1);

        Assert.IsTrue(Logic.Update(1, updated));

        Warehouse? stored = Logic.GetById(1);
        Assert.IsNotNull(stored);
        Assert.AreEqual("Updated Warehouse", stored.Name);
        Assert.AreEqual(TestData.CreatedAt, stored.CreatedAt);
        Assert.IsTrue(stored.UpdatedAt > TestData.UpdatedAt);
    }

    [TestMethod]
    public void UpdateReturnsFalseForMissingWarehouseAndRejectsInvalidData()
    {
        Warehouse invalid = TestData.CreateWarehouse();
        invalid.Name = string.Empty;

        Assert.IsFalse(Logic.Update(999, TestData.CreateWarehouse()));
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => Logic.Update(0, TestData.CreateWarehouse()));
        Assert.ThrowsException<ArgumentException>(() => Logic.Update(1, invalid));
        Assert.AreEqual("Test Warehouse", Logic.GetById(1)?.Name);
    }

    [TestMethod]
    public void RemoveDeletesWarehouseWithoutLocations()
    {
        Assert.IsTrue(Logic.Remove(1));
        Assert.IsNull(Logic.GetById(1));
        Assert.IsFalse(Logic.Remove(1));
    }

    [TestMethod]
    public void RemoveRefusesWarehouseThatStillHasLocations()
    {
        Assert.ThrowsException<InvalidOperationException>(() => Logic.Remove(2));
        Assert.IsNotNull(Logic.GetById(2));
    }

    [TestMethod]
    public void RemoveRefusesWarehouseThatOrdersStillUse()
    {
        Context.Orders.Add(new Order { Id = 1, WarehouseId = 1 });
        Context.SaveChanges();

        AssertConflict(() => Logic.Remove(1), "Warehouse 1 is still used by orders.");
        Assert.IsNotNull(Logic.GetById(1));
    }

    [TestMethod]
    public void UpdateRejectsAnIdThatDoesNotMatchTheUrl()
    {
        AssertInvalid(() => Logic.Update(1, TestData.CreateWarehouse(2)), "id 2 in the body does not match id 1 in the URL.");
        Assert.AreEqual("Test Warehouse", Logic.GetById(1)?.Name);
    }

    [TestMethod]
    public void RemoveRejectsInvalidIds()
    {
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => Logic.Remove(0));
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => Logic.Remove(-1));
    }
}
