using CargoHUB.Access;
using CargoHUB.Logics;
using CargoHUB.Models;
using CargoHUB.Tests.Infrastructure;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CargoHUB.Tests.Logics;

[TestClass]
public sealed class ShipmentItemLogicTests : DatabaseTest
{
    private ShipmentItemLogic Logic => new(new ShipmentItemDataAccess(Context), new InventoryDataAccess(Context));

    protected override void SeedDatabase()
    {
        Context.Inventories.AddRange(OrderShipmentTestData.CreateInventoriesForItem1());
        Context.Shipments.Add(OrderShipmentTestData.CreateShipment(1));
        Context.SaveChanges();
    }

    [TestMethod]
    public void GetByShipmentIdReturnsItemsSortedByItemId()
    {
        Logic.Replace(1, [OrderShipmentTestData.CreateShipmentItem(30, 1), OrderShipmentTestData.CreateShipmentItem(1, 3)]);

        CollectionAssert.AreEqual(new[] { 1, 30 }, Logic.GetByShipmentId(1).Select(item => item.ItemId).ToArray());
        Assert.AreEqual(0, Logic.GetByShipmentId(0).Count);
        Assert.AreEqual(0, Logic.GetByShipmentId(999).Count);
    }

    [TestMethod]
    public void ReplaceUpdatesOrderedQuantityOnTheLocationWithMostStock()
    {
        Logic.Replace(1, [OrderShipmentTestData.CreateShipmentItem(1, 3)]);

        Assert.AreEqual(5, Context.Inventories.Find(1, 2)!.QuantityOrdered);
        Assert.AreEqual(0, Context.Inventories.Find(1, 1)!.QuantityOrdered);

        Logic.Replace(1, []);

        Assert.AreEqual(2, Context.Inventories.Find(1, 2)!.QuantityOrdered);
        Assert.AreEqual(0, Logic.GetByShipmentId(1).Count);
    }

    [TestMethod]
    public void ReplaceNeverMakesOrderedQuantityNegative()
    {
        Context.Set<ShipmentItem>().Add(new ShipmentItem { ShipmentId = 1, ItemId = 1, Amount = 10 });
        Context.SaveChanges();

        Logic.Replace(1, []);

        Assert.AreEqual(0, Context.Inventories.Find(1, 2)!.QuantityOrdered);
    }

    [TestMethod]
    public void ReplaceForMissingShipmentChangesNothing()
    {
        Logic.Replace(999, [OrderShipmentTestData.CreateShipmentItem(1, 50)]);

        Assert.AreEqual(2, Context.Inventories.Find(1, 2)!.QuantityOrdered);
        Assert.AreEqual(0, Logic.GetByShipmentId(999).Count);
    }

    [TestMethod]
    public void ReplaceRejectsInvalidItems()
    {
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => Logic.Replace(0, []));
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => Logic.Replace(1, [OrderShipmentTestData.CreateShipmentItem(0, 1)]));
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => Logic.Replace(1, [OrderShipmentTestData.CreateShipmentItem(1, -2)]));
        Assert.ThrowsException<ArgumentException>(() => Logic.Replace(1, [OrderShipmentTestData.CreateShipmentItem(1, 1), OrderShipmentTestData.CreateShipmentItem(1, 2)]));
    }
}
