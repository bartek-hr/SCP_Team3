using CargoHUB.Access;
using CargoHUB.Logics;
using CargoHUB.Models;
using CargoHUB.Tests.Infrastructure;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CargoHUB.Tests.Logics;

[TestClass]
public sealed class OrderItemLogicTests : DatabaseTest
{
    private OrderItemLogic Logic => new(new OrderItemDataAccess(Context), new OrderDataAccess(Context), new ItemAccess(Context));

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
        var logic = Logic;

        logic.Update(1, new OrderItem
        {
            Id = 1,
            OrderId = 1,
            ItemId = 1,
            Amount = 4,
            UnitPrice = 6
        });

        Assert.AreEqual(4, logic.GetById(1)!.Amount);
        Assert.AreEqual(6, logic.GetById(1)!.UnitPrice);

    }

    [DataTestMethod]
    [DataRow(999, 1, 1, 1, "order_id 999 does not exist.")]
    [DataRow(1, 999, 1, 1, "item_id 999 does not exist.")]
    [DataRow(1, 2, 0, 1, "amount must be greater than 0.")]
    [DataRow(1, 2, 1, -1, "unit_price cannot be negative.")]
    public void OngeldigOrderItemWordtGeweigerd(int orderId, int itemId, int amount, int unitPrice, string message)
    {
        SeedVoorraad();

        AssertInvalid(() => Logic.Add(new OrderItem
        {
            OrderId = orderId,
            ItemId = itemId,
            Amount = amount,
            UnitPrice = unitPrice
        }), message);
        Assert.AreEqual(0, Logic.GetAll().Count);
    }

    [TestMethod]
    public void ArtikelMagMaarEenKeerInDeOrderStaan()
    {
        SeedVoorraad();
        Context.OrderItems.AddRange(
            new OrderItem { Id = 1, OrderId = 1, ItemId = 1, Amount = 1 },
            new OrderItem { Id = 2, OrderId = 1, ItemId = 2, Amount = 1 });
        Context.SaveChanges();

        AssertConflict(
            () => Logic.Add(new OrderItem { OrderId = 1, ItemId = 1, Amount = 3 }),
            "Order 1 already contains item_id 1.");
        AssertConflict(
            () => Logic.Update(2, new OrderItem { OrderId = 1, ItemId = 1, Amount = 3 }),
            "Order 1 already contains item_id 1.");
        AssertInvalid(
            () => Logic.Update(2, new OrderItem { Id = 1, OrderId = 1, ItemId = 2, Amount = 3 }),
            "id 1 in the body does not match id 2 in the URL.");

        Assert.AreEqual(2, Logic.GetById(2)!.ItemId);
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
        Context.Orders.Add(new Order
        {
            Id = 1
        });
        Context.SaveChanges();
    }
}
