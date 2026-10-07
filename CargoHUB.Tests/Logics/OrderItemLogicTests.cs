using CargoHUB.Access;
using CargoHUB.Logics;
using CargoHUB.Models;
using CargoHUB.Tests.Infrastructure;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CargoHUB.Tests.Logics;

[TestClass]
public sealed class OrderItemLogicTests : DatabaseTest
{
    private OrderItemLogic Logic => new(new OrderItemDataAccess(Context), new InventoryDataAccess(Context));

    protected override void SeedDatabase()
    {
        Context.Inventories.AddRange(TestData.CreateInventoriesForItem1());
        Context.Orders.Add(TestData.CreateOrder(1, TestData.CreateOrderItem(1, 4, 2.50m)));
        Context.SaveChanges();
    }

    [TestMethod]
    public void GetByOrderIdReturnsItemsSortedByItemId()
    {
        Logic.Replace(1, [TestData.CreateOrderItem(30, 1), TestData.CreateOrderItem(1, 4), TestData.CreateOrderItem(20, 2)]);

        CollectionAssert.AreEqual(new[] { 1, 20, 30 }, Logic.GetByOrderId(1).Select(item => item.ItemId).ToArray());
        Assert.AreEqual(0, Logic.GetByOrderId(0).Count);
        Assert.AreEqual(0, Logic.GetByOrderId(999).Count);
    }

    [TestMethod]
    public void ReplaceAllocatesTheDifferenceOnTheLocationWithMostStock()
    {
        Logic.Replace(1, [TestData.CreateOrderItem(1, 7, 3.00m)]);

        Assert.AreEqual(5, Context.Inventories.Find(1, 2)!.QuantityAllocated);
        Assert.AreEqual(1, Context.Inventories.Find(1, 1)!.QuantityAllocated);
        OrderItem stored = Logic.GetByOrderId(1).Single();
        Assert.AreEqual(7, stored.Amount);
        Assert.AreEqual(3.00m, stored.UnitPrice);
    }

    [TestMethod]
    public void ReplaceReleasesRemovedItemsButNeverBelowZero()
    {
        Logic.Replace(1, []);

        // 2 allocated - 4 released is clamped to 0, like the legacy API.
        Assert.AreEqual(0, Context.Inventories.Find(1, 2)!.QuantityAllocated);
        Assert.AreEqual(0, Logic.GetByOrderId(1).Count);
    }

    [TestMethod]
    public void ReplaceStoresItemsWithoutInventory()
    {
        Logic.Replace(1, [TestData.CreateOrderItem(1, 4), TestData.CreateOrderItem(99, 3)]);

        CollectionAssert.AreEqual(new[] { 1, 99 }, Logic.GetByOrderId(1).Select(item => item.ItemId).ToArray());
        Assert.AreEqual(2, Context.Inventories.Find(1, 2)!.QuantityAllocated);
    }

    [TestMethod]
    public void ReplaceForMissingOrderChangesNothing()
    {
        Logic.Replace(999, [TestData.CreateOrderItem(1, 50)]);

        Assert.AreEqual(2, Context.Inventories.Find(1, 2)!.QuantityAllocated);
        Assert.AreEqual(0, Logic.GetByOrderId(999).Count);
    }

    [TestMethod]
    public void ReplaceUpdatesTheOrderTimestamp()
    {
        Logic.Replace(1, [TestData.CreateOrderItem(1, 5)]);

        Assert.IsTrue(Context.Orders.Find(1)!.UpdatedAt > TestData.UpdatedAt);
    }

    [TestMethod]
    public void ReplaceRejectsInvalidItems()
    {
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => Logic.Replace(0, []));
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => Logic.Replace(1, [TestData.CreateOrderItem(0, 1)]));
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => Logic.Replace(1, [TestData.CreateOrderItem(1, 0)]));
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => Logic.Replace(1, [TestData.CreateOrderItem(1, 1, -1m)]));
        Assert.ThrowsException<ArgumentException>(() => Logic.Replace(1, [TestData.CreateOrderItem(1, 1), TestData.CreateOrderItem(1, 2)]));
        Assert.AreEqual(4, Logic.GetByOrderId(1).Single().Amount);
    }
}
