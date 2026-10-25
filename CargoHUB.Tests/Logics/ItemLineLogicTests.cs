using CargoHUB.Access;
using CargoHUB.Logics;
using CargoHUB.Models;
using CargoHUB.Tests.Infrastructure;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CargoHUB.Tests.Logics;

[TestClass]
public sealed class ItemLineLogicTests : DatabaseTest
{
    private ItemLineLogic Logic => new(new ItemLineAccess(Context), new ItemAccess(Context));

    protected override void SeedDatabase()
    {
        Context.ItemLines.AddRange(TestData.CreateItemLine(1), TestData.CreateItemLine(2, "Empty Line"));
        Context.ItemGroups.Add(TestData.CreateItemGroup(1));
        Context.Items.Add(TestData.CreateCatalogItem(1, itemLineId: 1, itemGroupId: 1));
        Context.SaveChanges();
    }

    [TestMethod]
    public void GetAllAndGetByIdReturnStoredItemLines()
    {
        CollectionAssert.AreEqual(new[] { 1, 2 }, Logic.GetAll().Select(itemLine => itemLine.Id).ToArray());
        Assert.AreEqual("Empty Line", Logic.GetById(2)?.Name);
        Assert.IsNull(Logic.GetById(999));
        Assert.IsNull(Logic.GetById(0));
        Assert.IsNull(Logic.GetById(-1));
    }

    [TestMethod]
    public void AddPersistsItemLineAndSetsTimestamps()
    {
        ItemLine itemLine = TestData.CreateItemLine(name: "New Line");

        Logic.Add(itemLine);

        ItemLine? stored = Logic.GetById(itemLine.Id);
        Assert.IsNotNull(stored);
        Assert.AreEqual("New Line", stored.Name);
        Assert.IsTrue(stored.CreatedAt > TestData.UpdatedAt);
        Assert.AreEqual(stored.CreatedAt, stored.UpdatedAt);
    }

    [TestMethod]
    public void AddRejectsInvalidDuplicateAndNegativeItemLines()
    {
        Assert.ThrowsException<ArgumentException>(() => Logic.Add(TestData.CreateItemLine(name: " ")));
        Assert.ThrowsException<ArgumentNullException>(() => Logic.Add(null!));
        Assert.ThrowsException<InvalidOperationException>(() => Logic.Add(TestData.CreateItemLine(1)));
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => Logic.Add(TestData.CreateItemLine(-1)));
        Assert.AreEqual(2, Logic.GetAll().Count);
    }

    [TestMethod]
    public void UpdatePreservesCreationTimeAndReturnsFalseForMissingItemLine()
    {
        ItemLine updated = TestData.CreateItemLine(1, "Renamed Line");
        updated.CreatedAt = DateTime.UtcNow;

        Assert.IsTrue(Logic.Update(1, updated));
        ItemLine? stored = Logic.GetById(1);
        Assert.AreEqual("Renamed Line", stored?.Name);
        Assert.AreEqual(TestData.CreatedAt, stored?.CreatedAt);
        Assert.IsTrue(stored?.UpdatedAt > TestData.UpdatedAt);
        Assert.ThrowsException<ArgumentException>(() => Logic.Update(1, TestData.CreateItemLine(name: "")));
        Assert.IsFalse(Logic.Update(999, TestData.CreateItemLine()));
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => Logic.Update(0, TestData.CreateItemLine()));
    }

    [TestMethod]
    public void RemoveDeletesEmptyItemLineAndRejectsLineWithItems()
    {
        Assert.ThrowsException<InvalidOperationException>(() => Logic.Remove(1));
        Assert.IsNotNull(Logic.GetById(1));

        Assert.IsTrue(Logic.Remove(2));
        Assert.IsNull(Logic.GetById(2));
        Assert.IsFalse(Logic.Remove(2));
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => Logic.Remove(0));
    }
}
