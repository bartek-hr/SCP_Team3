using CargoHUB.Access;
using CargoHUB.Logics;
using CargoHUB.Models;
using CargoHUB.Tests.Infrastructure;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CargoHUB.Tests.Logics;

[TestClass]
public sealed class OrderItemLogicTests : DatabaseTest
{
    [TestMethod]
    public void OrderItemKunnenWijzigen()
    {
        SeedVoorraad();
        Context.OrderItems.Add(new OrderItem
        {
            Id = 1,
            OrderId = 1,
            ItemId = 1,
            Amount = 1,
            UnitPrice = 1
        });
        Context.SaveChanges();
        var logic = new OrderItemLogic(new OrderItemDataAccess(Context));

        logic.Update(1, new OrderItem
        {
            Id = 1,
            OrderId = 1,
            ItemId = 1,
            Amount = 4,
            UnitPrice = 6
        });

        Assert.AreEqual(4, logic.GetById(1).Amount);
        Assert.AreEqual(6, logic.GetById(1).UnitPrice);

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
        Context.Orders.Add(new Order
        {
            Id = 1
        });
        Context.SaveChanges();
    }
}
