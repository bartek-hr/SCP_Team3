using CargoHUB.Access;
using CargoHUB.Logics;
using CargoHUB.Models;
using CargoHUB.Tests.Infrastructure;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CargoHUB.Tests.Logics;

[TestClass]
public sealed class VoorraadOrderShipmentTests : DatabaseTest
{
    [TestMethod]
    public void VoorraadUpsertBehoudtDeSleutelEnBerekentTotalen()
    {
        var logic = new InventoryLogic(new InventoryDataAccess(Context));
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
    public void OrderItemsWerkenGereserveerdeVoorraadBij()
    {
        SeedVoorraad();
        Context.Orders.Add(new Order
        {
            Id = 1,
            Items = [new OrderItem
            {
                ItemId = 1,
                Amount = 4
            }]
        });
        Context.SaveChanges();
        var logic = new OrderLogic(new OrderDataAccess(Context));

        logic.ReplaceItems(1, [new OrderItem
        {
            ItemId = 1,
            Amount = 7
        }]);
        Assert.AreEqual(5, Context.Inventories.Find(1, 2)!.QuantityAllocated);
        Assert.AreEqual(1, Context.Inventories.Find(1, 1)!.QuantityAllocated);
        logic.ReplaceItems(1, []);
        Assert.AreEqual(0, Context.Inventories.Find(1, 2)!.QuantityAllocated);
    }

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
