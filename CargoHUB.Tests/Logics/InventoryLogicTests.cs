using CargoHUB.Access;
using CargoHUB.Logics;
using CargoHUB.Models;
using CargoHUB.Tests.Infrastructure;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CargoHUB.Tests.Logics;

[TestClass]
public sealed class InventoryLogicTests : DatabaseTest
{
    private InventoryLogic Logic => new(new InventoryDataAccess(Context), new ItemAccess(Context), new LocationAccess(Context));

    protected override void SeedDatabase()
    {
        SeedCatalogItems(1);
        Context.Warehouses.Add(TestData.CreateWarehouse(1));
        Context.Locations.Add(TestData.CreateLocation(1));
        Context.SaveChanges();
    }

    [TestMethod]
    public void VoorraadUpsertBehoudtDeSleutelEnBerekentTotalen()
    {
        var logic = Logic;
        logic.AddOrUpdate(new Inventory
        {
            ItemId = 1,
            LocationId = 1,
            QuantityOnHand = 10
        });
        logic.AddOrUpdate(new Inventory
        {
            ItemId = 1,
            LocationId = 1,
            QuantityOnHand = 20,
            QuantityAllocated = 3
        });

        Assert.AreEqual(1, logic.GetAll().Count);
        Assert.AreEqual(17L, logic.GetTotalsByItemId(1).TotalAvailable);
        Assert.AreEqual(0L, logic.GetTotalsByItemId(999).TotalAvailable);
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => logic.Remove(0, 1));
    }

    [TestMethod]
    public void VoorraadVoorOnbekendArtikelOfLocatieWordtGeweigerd()
    {
        AssertInvalid(() => Logic.AddOrUpdate(new Inventory { ItemId = 999, LocationId = 1 }), "item_id 999 does not exist.");
        AssertInvalid(() => Logic.AddOrUpdate(new Inventory { ItemId = 1, LocationId = 999 }), "location_id 999 does not exist.");
        AssertInvalid(() => Logic.AddOrUpdate(new Inventory { LocationId = 1 }), "item_id 0 does not exist.");

        Assert.AreEqual(0, Logic.GetAll().Count);
    }

    [DataTestMethod]
    [DataRow(-1, 0, 0, 0, "quantity_on_hand cannot be negative.")]
    [DataRow(0, -1, 0, 0, "quantity_expected cannot be negative.")]
    [DataRow(0, 0, -1, 0, "quantity_ordered cannot be negative.")]
    [DataRow(0, 0, 0, -1, "quantity_allocated cannot be negative.")]
    public void NegatieveAantallenWordenGeweigerd(int onHand, int expected, int ordered, int allocated, string message)
    {
        var inventory = new Inventory
        {
            ItemId = 1,
            LocationId = 1,
            QuantityOnHand = onHand,
            QuantityExpected = expected,
            QuantityOrdered = ordered,
            QuantityAllocated = allocated
        };

        AssertInvalid(() => Logic.AddOrUpdate(inventory), message);
        Assert.AreEqual(0, Logic.GetAll().Count);
    }

    [TestMethod]
    public void WijzigenControleertDatDeSleutelBijDeUrlHoort()
    {
        Logic.AddOrUpdate(new Inventory { ItemId = 1, LocationId = 1, QuantityOnHand = 5 });

        AssertInvalid(
            () => Logic.Update(1, 1, new Inventory { ItemId = 1, LocationId = 2 }),
            "item_id and location_id in the body must match the URL.");
        Logic.Update(1, 1, new Inventory { ItemId = 1, LocationId = 1, QuantityOnHand = 8 });

        Assert.AreEqual(8, Logic.GetByKey(1, 1)?.QuantityOnHand);
    }
}
