using CargoHUB.Access;
using CargoHUB.Logics;
using CargoHUB.Models;
using CargoHUB.Tests.Infrastructure;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CargoHUB.Tests.Logics;

[TestClass]
public sealed class OrderLogicTests : DatabaseTest
{
    private OrderLogic Logic => new(new OrderDataAccess(Context),
        new OrderItemLogic(new OrderItemDataAccess(Context), new InventoryDataAccess(Context)));

    protected override void SeedDatabase()
    {
        Context.Inventories.AddRange(OrderShipmentTestData.CreateInventoriesForItem1());
        Context.Orders.Add(OrderShipmentTestData.CreateOrder(1, OrderShipmentTestData.CreateOrderItem(1, 4)));
        Context.SaveChanges();
    }

    [TestMethod]
    public void AddStoresTheItemsWithoutChangingStock()
    {
        Logic.Add(OrderShipmentTestData.CreateOrder(2, OrderShipmentTestData.CreateOrderItem(1, 3)));

        Assert.AreEqual(3, Logic.GetById(2)!.Items!.Single().Amount);
        Assert.AreEqual(2, Context.Inventories.Find(1, 2)!.QuantityAllocated);
    }

    [TestMethod]
    public void AddWithoutItemsStoresAnEmptyList()
    {
        Order order = OrderShipmentTestData.CreateOrder(2);
        order.Items = null;

        Logic.Add(order);

        Assert.AreEqual(0, Logic.GetById(2)!.Items!.Count);
    }

    [TestMethod]
    public void UpdateWithoutItemsKeepsTheItemsAndStock()
    {
        Order order = OrderShipmentTestData.CreateOrder(1);
        order.Items = null;
        order.Reference = "ORD-CHANGED";

        Logic.Update(1, order);

        Order stored = Logic.GetById(1)!;
        Assert.AreEqual("ORD-CHANGED", stored.Reference);
        Assert.AreEqual(4, stored.Items!.Single().Amount);
        Assert.AreEqual(2, Context.Inventories.Find(1, 2)!.QuantityAllocated);
    }

    [TestMethod]
    public void UpdateWithItemsReplacesThemAndUpdatesStock()
    {
        Logic.Update(1, OrderShipmentTestData.CreateOrder(1, OrderShipmentTestData.CreateOrderItem(1, 7)));

        Assert.AreEqual(7, Logic.GetById(1)!.Items!.Single().Amount);
        Assert.AreEqual(5, Context.Inventories.Find(1, 2)!.QuantityAllocated);
    }

    [TestMethod]
    public void RemoveDeletesTheOrderAndItsItems()
    {
        Logic.Remove(1);

        Assert.IsNull(Logic.GetById(1));
        Assert.AreEqual(0, Context.Set<OrderItem>().Count());
    }
}
