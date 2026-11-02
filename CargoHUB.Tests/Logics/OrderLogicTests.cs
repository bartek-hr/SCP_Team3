using CargoHUB.Access;
using CargoHUB.Logics;
using CargoHUB.Models;
using CargoHUB.Tests.Infrastructure;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CargoHUB.Tests.Logics;

[TestClass]
public sealed class OrderLogicTests : DatabaseTest
{
    private OrderLogic Logic => new(
        new OrderDataAccess(Context),
        new ClientAccess(Context),
        new WarehouseAccess(Context),
        new ItemAccess(Context),
        new ShipmentDataAccess(Context));

    protected override void SeedDatabase()
    {
        SeedCatalogItems(1, 2);
        Context.Clients.AddRange(TestData.CreateClient(1), TestData.CreateClient(2));
        Context.Warehouses.Add(TestData.CreateWarehouse(1));
        Context.SaveChanges();
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
        var logic = Logic;

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
    public void OrderMetBestaandeVerwijzingenWordtOpgeslagen()
    {
        Order order = TestData.CreateOrder(0, items: [TestData.CreateOrderItem(1, 2), TestData.CreateOrderItem(2, 1, null)]);
        order.ShipToClientId = 2;

        Logic.Add(order);

        Order? stored = Logic.GetById(order.Id);
        Assert.IsNotNull(stored);
        Assert.AreEqual(2, stored.ShipToClientId);
        Assert.AreEqual(2, stored.Items.Count);
    }

    [DataTestMethod]
    [DataRow("client_id", "client_id 999 does not exist.")]
    [DataRow("ship_to_client_id", "ship_to_client_id 999 does not exist.")]
    [DataRow("bill_to_client_id", "bill_to_client_id 999 does not exist.")]
    [DataRow("warehouse_id", "warehouse_id 999 does not exist.")]
    [DataRow("item_id", "items[0].item_id 999 does not exist.")]
    [DataRow("amount", "items[0].amount must be greater than 0.")]
    [DataRow("unit_price", "items[0].unit_price cannot be negative.")]
    [DataRow("duplicate_item", "item_id 1 appears more than once in items.")]
    [DataRow("request_date", "request_date cannot be before order_date.")]
    [DataRow("reference", "reference is required.")]
    [DataRow("order_status", "order_status is required.")]
    [DataRow("id", "id cannot be negative.")]
    public void OngeldigeOrderWordtGeweigerd(string field, string message)
    {
        Order order = TestData.CreateOrder(0, items: [TestData.CreateOrderItem(1, 2)]);
        switch (field)
        {
            case "client_id": order.ClientId = 999; break;
            case "ship_to_client_id": order.ShipToClientId = 999; break;
            case "bill_to_client_id": order.BillToClientId = 999; break;
            case "warehouse_id": order.WarehouseId = 999; break;
            case "item_id": order.Items[0].ItemId = 999; break;
            case "amount": order.Items[0].Amount = 0; break;
            case "unit_price": order.Items[0].UnitPrice = -1m; break;
            case "duplicate_item": order.Items.Add(TestData.CreateOrderItem(1, 3)); break;
            case "request_date": order.RequestDate = order.OrderDate.AddDays(-1); break;
            case "reference": order.Reference = " "; break;
            case "order_status": order.OrderStatus = string.Empty; break;
            case "id": order.Id = -1; break;
        }

        AssertInvalid(() => Logic.Add(order), message);
        Assert.AreEqual(0, Logic.GetAll().Count);
    }

    [TestMethod]
    public void ArtikelenVervangenControleertDeArtikelen()
    {
        SeedVoorraad();
        Context.Orders.Add(TestData.CreateOrder(1, items: [TestData.CreateOrderItem(1, 4)]));
        Context.SaveChanges();

        AssertInvalid(() => Logic.ReplaceItems(1, [TestData.CreateOrderItem(999, 1)]), "items[0].item_id 999 does not exist.");
        AssertInvalid(() => Logic.ReplaceItems(1, [TestData.CreateOrderItem(1, -2)]), "items[0].amount must be greater than 0.");

        Assert.AreEqual(4, Logic.GetItems(1).Single().Amount);
        Assert.AreEqual(2, Context.Inventories.Find(1, 2)!.QuantityAllocated);
    }

    [TestMethod]
    public void WijzigenControleertHetIdInDeBody()
    {
        Context.Orders.Add(TestData.CreateOrder(1));
        Context.SaveChanges();

        AssertInvalid(() => Logic.Update(1, TestData.CreateOrder(5)), "id 5 in the body does not match id 1 in the URL.");
        Logic.Update(1, TestData.CreateOrder(1, clientId: 2));

        Assert.AreEqual(2, Logic.GetById(1)?.ClientId);
    }

    [TestMethod]
    public void OrderMetZendingenKanNietWordenVerwijderd()
    {
        Context.Orders.AddRange(TestData.CreateOrder(1), TestData.CreateOrder(2));
        Context.Shipments.Add(TestData.CreateShipment(1, orderId: 1));
        Context.SaveChanges();

        AssertConflict(() => Logic.Remove(1), "Order 1 still has shipments.");
        Logic.Remove(2);

        Assert.IsNotNull(Logic.GetById(1));
        Assert.IsNull(Logic.GetById(2));
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
