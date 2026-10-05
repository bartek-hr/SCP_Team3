using CargoHUB.Access;
using CargoHUB.Logics;
using CargoHUB.Models;
using CargoHUB.Tests.Infrastructure;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CargoHUB.Tests.Logics;

[TestClass]
public sealed class TransferLogicTests : DatabaseTest
{
    private TransferLogic Logic => new(new TransferAccess(Context), new LocationAccess(Context));

    protected override void SeedDatabase()
    {
        Context.Warehouses.Add(TestData.CreateWarehouse(1));
        Context.Locations.AddRange(TestData.CreateLocation(1), TestData.CreateLocation(2), TestData.CreateLocation(3));

        Transfer processed = TestData.CreateTransfer(1, 1, 2, TestData.CreateItem(10, 5), TestData.CreateItem(20, 3));
        processed.TransferStatus = TransferStatus.Processed;
        Context.Transfers.AddRange(processed, TestData.CreateTransfer(2, 2, 3, TestData.CreateItem(30, 1)));
        Context.SaveChanges();
    }

    [TestMethod]
    public void GetAllAndGetByIdIncludeTheItems()
    {
        IReadOnlyList<Transfer> transfers = Logic.GetAll();

        CollectionAssert.AreEqual(new[] { 1, 2 }, transfers.Select(transfer => transfer.Id).ToArray());
        CollectionAssert.AreEqual(new[] { 10, 20 }, transfers[0].Items!.Select(item => item.ItemId).ToArray());
        Assert.AreEqual(TransferStatus.Processed, Logic.GetById(1)?.TransferStatus);
        Assert.AreEqual(1, Logic.GetById(2)?.Items?.Count);
        Assert.IsNull(Logic.GetById(999));
        Assert.IsNull(Logic.GetById(0));
    }

    [TestMethod]
    public void GetItemsReturnsItemsOrNullForMissingTransfer()
    {
        IReadOnlyList<TransferItem>? items = Logic.GetItems(1);

        Assert.IsNotNull(items);
        CollectionAssert.AreEqual(new[] { (10, 5), (20, 3) }, items.Select(item => (item.ItemId, item.Amount)).ToArray());
        Assert.IsNull(Logic.GetItems(999));
        Assert.IsNull(Logic.GetItems(0));
    }

    [TestMethod]
    public void AddSchedulesTheTransferAndStoresItsItems()
    {
        Transfer transfer = TestData.CreateTransfer(9001, 1, 3, TestData.CreateItem(1, 10), TestData.CreateItem(2, 5));
        transfer.TransferStatus = TransferStatus.Processed;

        Logic.Add(transfer);

        Transfer? stored = Logic.GetById(9001);
        Assert.IsNotNull(stored);
        Assert.AreEqual(TransferStatus.Scheduled, stored.TransferStatus);
        Assert.IsTrue(stored.CreatedAt > TestData.UpdatedAt);
        Assert.AreEqual(stored.CreatedAt, stored.UpdatedAt);
        CollectionAssert.AreEqual(new[] { (1, 10), (2, 5) }, stored.Items!.Select(item => (item.ItemId, item.Amount)).ToArray());
    }

    [TestMethod]
    public void AddWithoutItemsStoresAnEmptyItemList()
    {
        Transfer transfer = TestData.CreateTransfer(0, 1, 3);
        transfer.Items = null;

        Logic.Add(transfer);

        Assert.AreEqual(0, Logic.GetItems(transfer.Id)?.Count);
    }

    [TestMethod]
    public void AddRejectsInvalidTransfers()
    {
        Transfer missingReference = TestData.CreateTransfer(0, 1, 3);
        missingReference.Reference = " ";
        Transfer nullItem = TestData.CreateTransfer(0, 1, 3);
        nullItem.Items = [null!];

        Assert.ThrowsException<ArgumentNullException>(() => Logic.Add(null!));
        Assert.ThrowsException<ArgumentException>(() => Logic.Add(missingReference));
        Assert.ThrowsException<ArgumentException>(() => Logic.Add(TestData.CreateTransfer(0, 1, 1)));
        Assert.ThrowsException<ArgumentException>(() => Logic.Add(TestData.CreateTransfer(0, 1, 999)));
        Assert.ThrowsException<ArgumentException>(() => Logic.Add(TestData.CreateTransfer(0, 0, 1)));
        Assert.ThrowsException<ArgumentException>(() => Logic.Add(TestData.CreateTransfer(0, 1, 3, TestData.CreateItem(1, 0))));
        Assert.ThrowsException<ArgumentException>(() => Logic.Add(TestData.CreateTransfer(0, 1, 3, TestData.CreateItem(0, 5))));
        Assert.ThrowsException<ArgumentException>(() => Logic.Add(nullItem));
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => Logic.Add(TestData.CreateTransfer(-1, 1, 3)));
        Assert.ThrowsException<InvalidOperationException>(() => Logic.Add(TestData.CreateTransfer(1, 1, 3)));
        Assert.AreEqual(2, Logic.GetAll().Count);
    }

    [TestMethod]
    public void UpdateReplacesItemsWhenTheyAreSent()
    {
        Transfer updated = TestData.CreateTransfer(0, 2, 3, TestData.CreateItem(99, 7));
        updated.Reference = "TRF-UPDATED";

        Assert.IsTrue(Logic.Update(1, updated));

        Transfer? stored = Logic.GetById(1);
        Assert.IsNotNull(stored);
        Assert.AreEqual("TRF-UPDATED", stored.Reference);
        Assert.AreEqual(2, stored.FromLocationId);
        Assert.AreEqual(3, stored.ToLocationId);
        CollectionAssert.AreEqual(new[] { (99, 7) }, stored.Items!.Select(item => (item.ItemId, item.Amount)).ToArray());
        Assert.AreEqual(1, Context.TransferItems.Count(item => item.TransferId == 1));
    }

    [TestMethod]
    public void UpdateKeepsTheStoredItemsWhenTheyAreOmitted()
    {
        Transfer updated = TestData.CreateTransfer(0, 1, 3);
        updated.Items = null;

        Assert.IsTrue(Logic.Update(1, updated));

        Transfer? stored = Logic.GetById(1);
        Assert.IsNotNull(stored);
        Assert.AreEqual(3, stored.ToLocationId);
        CollectionAssert.AreEqual(new[] { 10, 20 }, stored.Items!.Select(item => item.ItemId).ToArray());
    }

    [TestMethod]
    public void UpdateKeepsStatusAndCreationTime()
    {
        Transfer updated = TestData.CreateTransfer(0, 2, 3);
        updated.TransferStatus = TransferStatus.Processed;
        updated.CreatedAt = DateTime.UtcNow.AddYears(1);

        Assert.IsTrue(Logic.Update(2, updated));

        Transfer? stored = Logic.GetById(2);
        Assert.IsNotNull(stored);
        Assert.AreEqual(TransferStatus.Scheduled, stored.TransferStatus);
        Assert.AreEqual(TestData.CreatedAt, stored.CreatedAt);
        Assert.IsTrue(stored.UpdatedAt > TestData.UpdatedAt);
    }

    [TestMethod]
    public void UpdateReturnsFalseForMissingTransferAndRejectsInvalidData()
    {
        Assert.IsFalse(Logic.Update(999, TestData.CreateTransfer(0, 1, 3)));
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => Logic.Update(0, TestData.CreateTransfer(0, 1, 3)));
        Assert.ThrowsException<ArgumentException>(() => Logic.Update(1, TestData.CreateTransfer(0, 1, 999)));
        Assert.AreEqual(2, Logic.GetById(1)?.ToLocationId);
    }

    [TestMethod]
    public void RemoveDeletesTransferTogetherWithItsItems()
    {
        Assert.IsTrue(Logic.Remove(1));

        Assert.IsNull(Logic.GetById(1));
        Assert.AreEqual(0, Context.TransferItems.Count(item => item.TransferId == 1));
        Assert.AreEqual(1, Context.TransferItems.Count(item => item.TransferId == 2));
        Assert.IsFalse(Logic.Remove(1));
    }

    [TestMethod]
    public void RemoveRejectsInvalidIds()
    {
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => Logic.Remove(0));
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => Logic.Remove(-1));
    }
}
