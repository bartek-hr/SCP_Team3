using CargoHUB.Access;
using CargoHUB.Logics;
using CargoHUB.Models;
using CargoHUB.Tests.Infrastructure;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CargoHUB.Tests.Logics;

[TestClass]
public sealed class ShipmentItemLogicTests : DatabaseTest
{
    private ShipmentItemLogic Logic => new(new ShipmentItemDataAccess(Context), new ShipmentDataAccess(Context), new ItemAccess(Context));

    [TestMethod]
    public void ShipmentItemsKunnenWijzigen()
    {
        SeedVoorraad();
        Context.ShipmentItems.Add(new ShipmentItem
        {
            Id = 1,
            ShipmentId = 1,
            ItemId = 1,
            Amount = 1
        });
        Context.SaveChanges();
        var logic = Logic;

        logic.Update(1, new ShipmentItem
        {
            Id = 1,
            ShipmentId = 1,
            ItemId = 1,
            Amount = 4
        });

        Assert.AreEqual(4, logic.GetById(1)!.Amount);

    }

    [DataTestMethod]
    [DataRow(999, 1, 1, "shipment_id 999 does not exist.")]
    [DataRow(0, 1, 1, "shipment_id 0 does not exist.")]
    [DataRow(1, 999, 1, "item_id 999 does not exist.")]
    [DataRow(1, 2, -3, "amount must be greater than 0.")]
    public void OngeldigShipmentItemWordtGeweigerd(int shipmentId, int itemId, int amount, string message)
    {
        SeedVoorraad();

        AssertInvalid(() => Logic.Add(new ShipmentItem
        {
            ShipmentId = shipmentId,
            ItemId = itemId,
            Amount = amount
        }), message);
        Assert.AreEqual(0, Logic.GetItems(1).Count);
    }

    [TestMethod]
    public void ArtikelMagMaarEenKeerInDeZendingStaan()
    {
        SeedVoorraad();
        Context.ShipmentItems.Add(new ShipmentItem { Id = 1, ShipmentId = 1, ItemId = 1, Amount = 1 });
        Context.SaveChanges();

        AssertConflict(
            () => Logic.Add(new ShipmentItem { ShipmentId = 1, ItemId = 1, Amount = 2 }),
            "Shipment 1 already contains item_id 1.");
        Logic.Add(new ShipmentItem { ShipmentId = 1, ItemId = 2, Amount = 2 });

        Assert.AreEqual(2, Logic.GetItems(1).Count);
    }

    private void SeedVoorraad()
    {
        SeedCatalogItems(1, 2);
        Context.Inventories.AddRange(new Inventory
        {
            ItemId = 1,
            LocationId = 1,
            QuantityOnHand = 5,
            QuantityAllocated = 1
        }, new Inventory
        {
            ItemId = 1,
            LocationId = 2,
            QuantityOnHand = 10,
            QuantityAllocated = 2,
            QuantityOrdered = 2
        });
        Context.Shipments.Add(new Shipment
        {
            Id = 1
        });
        Context.SaveChanges();
    }
}
