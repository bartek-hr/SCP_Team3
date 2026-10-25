using CargoHUB.Access;
using CargoHUB.Logics;
using CargoHUB.Models;
using CargoHUB.Tests.Infrastructure;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CargoHUB.Tests.Logics;

[TestClass]
public sealed class TransferItemLogicTests : DatabaseTest
{
    [TestMethod]
    public void TransferItemsKunnenWijzigen()
    {
        SeedVoorraad();
        Context.TransferItems.Add(new TransferItem
        {
            Id = 1,
            TransferId = 1,
            ItemId = 1,
            Amount = 1
        });
        Context.SaveChanges();
        var logic = new TransferItemLogic(new TransferItemAccess(Context));

        logic.Update(1, new TransferItem
        {
            Id = 1,
            TransferId = 1,
            ItemId = 1,
            Amount = 4
        });

        Assert.AreEqual(4, logic.GetById(1).Amount);

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
        Context.Warehouses.Add(TestData.CreateWarehouse(1));
        Context.Locations.AddRange(TestData.CreateLocation(1), TestData.CreateLocation(2), TestData.CreateLocation(3));

        Context.Transfers.Add(TestData.CreateTransfer(1, 1, 2));
        Context.SaveChanges();
    }
}
