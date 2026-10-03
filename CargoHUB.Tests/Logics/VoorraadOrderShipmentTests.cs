using CargoHUB.Access;
using CargoHUB.Logics;
using CargoHUB.Models;
using CargoHUB.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CargoHUB.Tests.Logics;

[TestClass]
public sealed class VoorraadOrderShipmentTests : DatabaseTest
{
    private InventoryLogic Inventory => new(new InventoryDataAccess(Context));
    private OrderLogic Orders => new(new OrderDataAccess(Context));
    private ShipmentLogic Shipments => new(new ShipmentDataAccess(Context));

    [TestMethod]
    public void VoorraadBehoudtSamengesteldeSleutelEnBerekentTotalen()
    {
        Inventory.AddOrUpdate(new Inventory { ItemId = 1, LocationId = 1, QuantityOnHand = 10 });
        DateTime created = Inventory.GetByKey(1, 1)!.CreatedAt;
        Inventory.AddOrUpdate(new Inventory { ItemId = 1, LocationId = 1, QuantityOnHand = 20, QuantityAllocated = 3 });
        Inventory.AddOrUpdate(new Inventory { ItemId = 1, LocationId = 2, QuantityOnHand = 5, QuantityExpected = 4, QuantityOrdered = 2 });

        Assert.AreEqual(2, Inventory.GetAll().Count);
        Assert.AreEqual(created, Inventory.GetByKey(1, 1)!.CreatedAt);
        InventoryTotals totals = Inventory.GetTotalsByItemId(1);
        Assert.AreEqual(22L, totals.TotalAvailable);
        Assert.AreEqual(3L, totals.TotalAllocated);
        Assert.AreEqual(4L, totals.TotalExpected);
        Assert.AreEqual(2L, totals.TotalOrdered);

        Inventory.Update(1, 2, new Inventory { ItemId = 1, LocationId = 2, QuantityOnHand = 8 });
        Assert.AreEqual(8, Inventory.GetByKey(1, 2)!.QuantityOnHand);
        Inventory.Remove(1, 1);
        Assert.AreEqual(1, Inventory.GetByItemId(1).Count);
        Assert.IsNull(Inventory.GetByKey(1, 1));
        Assert.AreEqual(0L, Inventory.GetTotalsByItemId(999).TotalAvailable);
    }

    [TestMethod]
    public void VoorraadWeigertNegatieveAantallenEnAfwijkendeSleutels()
    {
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => Inventory.AddOrUpdate(
            new Inventory { ItemId = 1, LocationId = 1, QuantityOnHand = -1 }));
        Assert.ThrowsException<ArgumentException>(() => Inventory.Update(1, 2,
            new Inventory { ItemId = 1, LocationId = 3 }));
        Assert.IsNull(Inventory.GetByKey(0, 1));
        Assert.AreEqual(0, Inventory.GetAll().Count);
    }

    [TestMethod]
    public void OrderwijzigingenVerwerkenAlleenHetVerschilInGereserveerdeVoorraad()
    {
        SeedVoorraad();
        Order order = NieuweOrder();
        Orders.Add(order);
        Orders.ReplaceItems(order.Id, [new OrderItem { ItemId = 1, Amount = 7 }, new OrderItem { ItemId = 2, Amount = 2 }]);
        Assert.AreEqual(5, Context.Inventories.Find(1, 2)!.QuantityAllocated);
        Assert.AreEqual(1, Context.Inventories.Find(1, 1)!.QuantityAllocated);
        Assert.AreEqual(2, Orders.GetItems(order.Id).Count);

        Order updated = Orders.GetById(order.Id)!;
        updated.Items = [];
        Orders.Update(order.Id, updated);
        Assert.AreEqual(0, Context.Inventories.Find(1, 2)!.QuantityAllocated);
        Assert.AreEqual(0, Orders.GetItems(order.Id).Count);
        Assert.AreEqual(1, Orders.GetByClientId(3).Count);
        Orders.Remove(order.Id);
        Assert.IsNull(Orders.GetById(order.Id));
    }

    [TestMethod]
    public void OrderWeigertDubbeleArtikelenZonderDeVoorraadTeWijzigen()
    {
        SeedVoorraad();
        Order order = NieuweOrder();
        Orders.Add(order);
        Assert.ThrowsException<ArgumentException>(() => Orders.ReplaceItems(order.Id,
            [new OrderItem { ItemId = 1, Amount = 2 }, new OrderItem { ItemId = 1, Amount = 3 }]));
        Assert.AreEqual(4, Orders.GetItems(order.Id).Single().Amount);
        Assert.AreEqual(2, Context.Inventories.Find(1, 2)!.QuantityAllocated);
        Orders.ReplaceItems(999, []);
        Assert.IsNull(Orders.GetById(999));
    }

    [TestMethod]
    public void ShipmentwijzigingenVerwerkenVoorraadEnKunnenOrderkoppelingWissen()
    {
        SeedVoorraad();
        Shipment shipment = NieuweShipment();
        Shipments.Add(shipment);
        Shipments.ReplaceItems(shipment.Id, [new ShipmentItem { ItemId = 1, Amount = 7 }, new ShipmentItem { ItemId = 2, Amount = 2 }]);
        Assert.AreEqual(5, Context.Inventories.Find(1, 2)!.QuantityOrdered);
        Assert.AreEqual(1, Context.Inventories.Find(1, 1)!.QuantityOrdered);

        Shipment updated = Shipments.GetById(shipment.Id)!;
        updated.Items = [];
        Shipments.Update(shipment.Id, updated);
        Assert.AreEqual(0, Context.Inventories.Find(1, 2)!.QuantityOrdered);
        Assert.AreEqual(0, Shipments.GetItems(shipment.Id).Count);
        Shipments.ReplaceOrders(shipment.Id, [12, 13]);
        CollectionAssert.AreEqual(new[] { 12 }, Shipments.GetOrderIds(shipment.Id).ToArray());
        Shipments.ReplaceOrders(shipment.Id, []);
        Assert.AreEqual(0, Shipments.GetOrderIds(shipment.Id).Count);
        Shipments.Remove(shipment.Id);
        Assert.IsNull(Shipments.GetById(shipment.Id));
    }

    [TestMethod]
    public void ShipmentWeigertOngeldigeArtikelenEnOrderIds()
    {
        Shipment shipment = NieuweShipment();
        Shipments.Add(shipment);
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => Shipments.ReplaceItems(shipment.Id,
            [new ShipmentItem { ItemId = 1, Amount = 0 }]));
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => Shipments.ReplaceOrders(shipment.Id, [-1]));
        Assert.AreEqual(4, Shipments.GetItems(shipment.Id).Single().Amount);
        Shipments.ReplaceItems(999, []);
        Assert.IsNull(Shipments.GetById(999));
    }

    private void SeedVoorraad()
    {
        Context.Inventories.AddRange(
            new Inventory { ItemId = 1, LocationId = 1, QuantityOnHand = 5, QuantityAllocated = 1, QuantityOrdered = 1 },
            new Inventory { ItemId = 1, LocationId = 2, QuantityOnHand = 10, QuantityAllocated = 2, QuantityOrdered = 2 });
        Context.SaveChanges();
    }

    private static Order NieuweOrder() => new()
    {
        ClientId = 1, ShipToClientId = 2, BillToClientId = 3, WarehouseId = 1,
        OrderDate = DateTime.UtcNow, RequestDate = DateTime.UtcNow,
        Reference = "TEST", CustomerPoNumber = "PO-1", OrderStatus = "Pending",
        Items = [new OrderItem { ItemId = 1, Amount = 4 }]
    };

    private static Shipment NieuweShipment() => new()
    {
        Reference = "TEST", ShipmentDate = DateTime.UtcNow, ShipmentType = "Outgoing",
        ShipmentStatus = "Scheduled", CarrierName = "PostNL", ShippingMethod = "Standard", PaymentType = "Automated",
        Items = [new ShipmentItem { ItemId = 1, Amount = 4 }]
    };
}
