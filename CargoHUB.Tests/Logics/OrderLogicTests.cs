using CargoHUB.Access;
using CargoHUB.Logics;
using CargoHUB.Models;
using CargoHUB.Tests.Infrastructure;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CargoHUB.Tests.Logics;

[TestClass]
public sealed class OrderLogicTests : DatabaseTest
{
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
