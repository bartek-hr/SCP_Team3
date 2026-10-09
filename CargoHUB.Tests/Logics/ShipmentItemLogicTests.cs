using CargoHUB.Access;
using CargoHUB.Logics;
using CargoHUB.Models;
using CargoHUB.Tests.Infrastructure;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CargoHUB.Tests.Logics;

[TestClass]
public sealed class ShipmentItemLogicTests : DatabaseTest
{
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
        var logic = new ShipmentItemLogic(new ShipmentItemDataAccess(Context));

        logic.Update(1, new ShipmentItem
        {
            Id = 1,
            ShipmentId = 1,
            ItemId = 1,
            Amount = 4
        });

        Assert.AreEqual(4, logic.GetById(1).Amount);

    }

    private void SeedVoorraad()
    {
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
