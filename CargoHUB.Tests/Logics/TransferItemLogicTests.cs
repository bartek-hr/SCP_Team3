using CargoHUB.Access;
using CargoHUB.Logics;
using CargoHUB.Models;
using CargoHUB.Tests.Infrastructure;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CargoHUB.Tests.Logics;

[TestClass]
public sealed class TransferItemLogicTests : DatabaseTest
{
    private TransferItemLogic Logic => new(new TransferItemAccess(Context), new TransferAccess(Context), new ItemAccess(Context));

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
        var logic = Logic;

        logic.Update(1, new TransferItem
        {
            Id = 1,
            TransferId = 1,
            ItemId = 1,
            Amount = 4
        });

        Assert.AreEqual(4, logic.GetById(1)!.Amount);

    }

    [DataTestMethod]
    [DataRow(0, 1, 1, "transfer_id 0 does not exist.")]
    [DataRow(999, 1, 1, "transfer_id 999 does not exist.")]
    [DataRow(1, 999, 1, "item_id 999 does not exist.")]
    [DataRow(1, 1, 0, "amount must be greater than 0.")]
    public void OngeldigTransferItemWordtGeweigerd(int transferId, int itemId, int amount, string message)
    {
        SeedVoorraad();

        AssertInvalid(() => Logic.Add(new TransferItem
        {
            TransferId = transferId,
            ItemId = itemId,
            Amount = amount
        }), message);
        Assert.AreEqual(0, Logic.GetItems(1).Count);
    }

    [TestMethod]
    public void ArtikelMagMaarEenKeerInDeTransferStaan()
    {
        SeedVoorraad();
        Context.TransferItems.Add(new TransferItem { Id = 1, TransferId = 1, ItemId = 1, Amount = 1 });
        Context.SaveChanges();

        AssertConflict(
            () => Logic.Add(new TransferItem { TransferId = 1, ItemId = 1, Amount = 2 }),
            "Transfer 1 already contains item_id 1.");

        Assert.AreEqual(1, Logic.GetItems(1).Count);
    }

    [TestMethod]
    public void ItemsVanEenVerwerkteTransferLiggenVast()
    {
        SeedVoorraad();
        Transfer processed = TestData.CreateTransfer(2, 2, 3);
        processed.TransferStatus = TransferStatus.Processed;
        Context.Transfers.Add(processed);
        Context.TransferItems.Add(new TransferItem { Id = 2, TransferId = 2, ItemId = 1, Amount = 1 });
        Context.SaveChanges();
        const string message = "Transfer 2 is already processed and cannot be changed.";

        AssertConflict(() => Logic.Add(new TransferItem { TransferId = 2, ItemId = 2, Amount = 1 }), message);
        AssertConflict(() => Logic.Update(2, new TransferItem { TransferId = 2, ItemId = 1, Amount = 5 }), message);
        AssertConflict(() => Logic.Remove(2), message);

        Assert.AreEqual(1, Logic.GetById(2)!.Amount);
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
        Context.Warehouses.Add(TestData.CreateWarehouse(1));
        Context.Locations.AddRange(TestData.CreateLocation(1), TestData.CreateLocation(2), TestData.CreateLocation(3));

        Context.Transfers.Add(TestData.CreateTransfer(1, 1, 2));
        Context.SaveChanges();
    }
}
