using CargoHUB.Access;
using CargoHUB.Logics;
using CargoHUB.Models;
using CargoHUB.Tests.Infrastructure;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CargoHUB.Tests.Logics;

[TestClass]
public sealed class SupplierLogicTests : DatabaseTest
{
    private SupplierLogic Logic => new(new SupplierAccess(Context));

    protected override void SeedDatabase()
    {
        Context.Suppliers.Add(CreateSupplier(1));
        Context.SaveChanges();
    }

    [TestMethod]
    public void GetAllAndGetByIdReturnStoredSuppliers()
    {
        Assert.AreEqual(1, Logic.GetAll().Count);
        Assert.AreEqual("SUP-0001", Logic.GetById(1)?.Code);
        Assert.IsNull(Logic.GetById(999));
        Assert.IsNull(Logic.GetById(0));
    }

    [TestMethod]
    public void AddPersistsSupplierAndSetsTimestamps()
    {
        Supplier supplier = CreateSupplier();
        Logic.Add(supplier);

        Supplier? stored = Logic.GetById(supplier.Id);
        Assert.IsNotNull(stored);
        Assert.IsTrue(stored.CreatedAt > DateTime.UnixEpoch);
        Assert.AreEqual(stored.CreatedAt, stored.UpdatedAt);
    }

    [TestMethod]
    public void AddRejectsInvalidAndDuplicateSuppliers()
    {
        Supplier invalid = CreateSupplier();
        invalid.PhoneNumber = string.Empty;

        Assert.ThrowsException<ArgumentException>(() => Logic.Add(invalid));
        Assert.ThrowsException<InvalidOperationException>(() => Logic.Add(CreateSupplier(1)));
    }

    [TestMethod]
    public void UpdatePreservesCreationTimeAndReturnsFalseForMissingSupplier()
    {
        DateTime createdAt = Logic.GetById(1)!.CreatedAt;
        Supplier updated = CreateSupplier(1);
        updated.Name = "Updated Supplier";

        Assert.IsTrue(Logic.Update(1, updated));
        Assert.AreEqual("Updated Supplier", Logic.GetById(1)?.Name);
        Assert.AreEqual(createdAt, Logic.GetById(1)?.CreatedAt);
        Assert.IsTrue(Logic.GetById(1)?.UpdatedAt > createdAt);
        Assert.IsFalse(Logic.Update(999, CreateSupplier()));
    }

    [TestMethod]
    public void RemoveDeletesSupplierAndReturnsFalseForMissingSupplier()
    {
        Assert.IsFalse(Logic.Remove(999));
        Assert.IsTrue(Logic.Remove(1));
        Assert.IsNull(Logic.GetById(1));
    }

    private static Supplier CreateSupplier(int id = 0) => new()
    {
        Id = id,
        Code = "SUP-0001",
        Name = "Test Supplier",
        Address = "Test Street 1",
        City = "Amersfoort",
        ZipCode = "3811AA",
        Province = "Utrecht",
        Country = "Netherlands",
        ContactName = "Test Contact",
        PhoneNumber = "+31 600000000",
        Reference = "Test",
        CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
        UpdatedAt = new DateTime(2024, 1, 2, 0, 0, 0, DateTimeKind.Utc),
    };
}
