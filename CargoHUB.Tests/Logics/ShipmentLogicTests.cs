using CargoHUB.Access;
using CargoHUB.Logics;
using CargoHUB.Models;
using CargoHUB.Tests.Infrastructure;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CargoHUB.Tests.Logics;

[TestClass]
public sealed class ShipmentLogicTests : DatabaseTest
{
    private ShipmentLogic Logic => new(new ShipmentDataAccess(Context),
        new ShipmentItemLogic(new ShipmentItemDataAccess(Context), new InventoryDataAccess(Context)));

    protected override void SeedDatabase()
    {
        Context.Inventories.AddRange(OrderShipmentTestData.CreateInventoriesForItem1());
        Context.Shipments.Add(OrderShipmentTestData.CreateShipment(1, OrderShipmentTestData.CreateShipmentItem(1, 3)));
        Context.SaveChanges();
    }

    [TestMethod]
    public void UpdateWithoutItemsKeepsTheItemsAndStock()
    {
        Shipment shipment = OrderShipmentTestData.CreateShipment(1);
        shipment.Items = null;
        shipment.CarrierName = "PostNL";

        Logic.Update(1, shipment);

        Shipment stored = Logic.GetById(1)!;
        Assert.AreEqual("PostNL", stored.CarrierName);
        Assert.AreEqual(3, stored.Items!.Single().Amount);
        Assert.AreEqual(2, Context.Inventories.Find(1, 2)!.QuantityOrdered);
    }

    [TestMethod]
    public void UpdateWithItemsReplacesThemAndUpdatesStock()
    {
        Logic.Update(1, OrderShipmentTestData.CreateShipment(1, OrderShipmentTestData.CreateShipmentItem(1, 5)));

        Assert.AreEqual(5, Logic.GetById(1)!.Items!.Single().Amount);
        Assert.AreEqual(4, Context.Inventories.Find(1, 2)!.QuantityOrdered);
    }

    [TestMethod]
    public void ReplaceOrdersKeepsOnlyTheFirstOrder()
    {
        Logic.ReplaceOrders(1, [12, 13]);
        CollectionAssert.AreEqual(new[] { 12 }, Logic.GetOrderIds(1).ToArray());

        Logic.ReplaceOrders(1, []);
        Assert.AreEqual(0, Logic.GetOrderIds(1).Count);
    }
}
