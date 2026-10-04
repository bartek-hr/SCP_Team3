using CargoHUB.Access;
using CargoHUB.Logics;
using CargoHUB.Models;
using CargoHUB.Tests.Infrastructure;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CargoHUB.Tests.Logics;

[TestClass]
public sealed class ShipmentLogicTests : DatabaseTest
{
    [TestMethod]
    public void ShipmentItemsEnOrderkoppelingKunnenWijzigen()
    {
        SeedVoorraad();
        Context.Shipments.Add(new Shipment
        {
            Id = 1
        });
        Context.SaveChanges();
        var logic = new ShipmentLogic(new ShipmentDataAccess(Context));

        logic.ReplaceItems(1, [new ShipmentItem
        {
            ItemId = 1,
            Amount = 3
        }]);
        Assert.AreEqual(5, Context.Inventories.Find(1, 2)!.QuantityOrdered);
        logic.ReplaceItems(1, []);
        Assert.AreEqual(2, Context.Inventories.Find(1, 2)!.QuantityOrdered);
        logic.ReplaceOrders(1, [12, 13]);
        CollectionAssert.AreEqual(new[] { 12 }, logic.GetOrderIds(1).ToArray());
        logic.ReplaceOrders(1, []);
        Assert.AreEqual(0, logic.GetOrderIds(1).Count);
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
        Context.SaveChanges();
    }
}
