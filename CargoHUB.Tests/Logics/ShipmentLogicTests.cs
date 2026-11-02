using CargoHUB.Access;
using CargoHUB.Logics;
using CargoHUB.Models;
using CargoHUB.Tests.Infrastructure;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CargoHUB.Tests.Logics;

[TestClass]
public sealed class ShipmentLogicTests : DatabaseTest
{
    private ShipmentLogic Logic => new(new ShipmentDataAccess(Context), new OrderDataAccess(Context), new ItemAccess(Context));

    protected override void SeedDatabase()
    {
        SeedCatalogItems(1, 2);
        Context.Orders.AddRange(new Order
        {
            Id = 12
        }, new Order
        {
            Id = 13
        });
        Context.SaveChanges();
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
        var logic = Logic;

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

    [TestMethod]
    public void ZendingMetEnZonderOrderWordtOpgeslagen()
    {
        Logic.Add(TestData.CreateShipment(0, orderId: 12, TestData.CreateShipmentItem(1, 2)));
        Logic.Add(TestData.CreateShipment(0, orderId: null));

        Assert.AreEqual(2, Logic.GetAll().Count);
        Assert.AreEqual(12, Logic.GetAll()[0].OrderId);
    }

    [DataTestMethod]
    [DataRow("order_id", "order_id 999 does not exist.")]
    [DataRow("item_id", "items[0].item_id 999 does not exist.")]
    [DataRow("amount", "items[0].amount must be greater than 0.")]
    [DataRow("duplicate_item", "item_id 1 appears more than once in items.")]
    [DataRow("shipment_status", "shipment_status is required.")]
    [DataRow("shipment_date", "shipment_date is required.")]
    public void OngeldigeZendingWordtGeweigerd(string field, string message)
    {
        Shipment shipment = TestData.CreateShipment(0, orderId: 12, TestData.CreateShipmentItem(1, 2));
        switch (field)
        {
            case "order_id": shipment.OrderId = 999; break;
            case "item_id": shipment.Items[0].ItemId = 999; break;
            case "amount": shipment.Items[0].Amount = 0; break;
            case "duplicate_item": shipment.Items.Add(TestData.CreateShipmentItem(1, 1)); break;
            case "shipment_status": shipment.ShipmentStatus = " "; break;
            case "shipment_date": shipment.ShipmentDate = default; break;
        }

        AssertInvalid(() => Logic.Add(shipment), message);
        Assert.AreEqual(0, Logic.GetAll().Count);
    }

    [TestMethod]
    public void OrderkoppelingControleertDatDeOrdersBestaan()
    {
        Context.Shipments.Add(TestData.CreateShipment(1, orderId: 12));
        Context.SaveChanges();

        AssertInvalid(() => Logic.ReplaceOrders(1, [13, 999]), "order_id 999 does not exist.");
        AssertInvalid(() => Logic.ReplaceOrders(1, [0]), "order_id 0 does not exist.");

        CollectionAssert.AreEqual(new[] { 12 }, Logic.GetOrderIds(1).ToArray());
    }

    [TestMethod]
    public void WijzigenControleertIdEnArtikelen()
    {
        Context.Shipments.Add(TestData.CreateShipment(1));
        Context.SaveChanges();

        AssertInvalid(() => Logic.Update(1, TestData.CreateShipment(2)), "id 2 in the body does not match id 1 in the URL.");
        AssertInvalid(() => Logic.ReplaceItems(1, [TestData.CreateShipmentItem(999, 1)]), "items[0].item_id 999 does not exist.");
        Logic.Update(1, TestData.CreateShipment(1, orderId: 13));

        Assert.AreEqual(13, Logic.GetById(1)?.OrderId);
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
