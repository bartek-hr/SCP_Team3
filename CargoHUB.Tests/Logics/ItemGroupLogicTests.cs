using CargoHUB.Access;
using CargoHUB.Logics;
using CargoHUB.Models;
using CargoHUB.Tests.Infrastructure;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CargoHUB.Tests.Logics;

[TestClass]
public sealed class ItemGroupLogicTests : DatabaseTest
{
    private ItemGroupLogic Logic => new(new ItemGroupAccess(Context), new ItemAccess(Context));

    protected override void SeedDatabase()
    {
        Context.ItemLines.Add(TestData.CreateItemLine(1));
        Context.ItemGroups.AddRange(TestData.CreateItemGroup(1), TestData.CreateItemGroup(2, "Empty Group"));
        Context.Items.Add(TestData.CreateCatalogItem(1, itemLineId: 1, itemGroupId: 1));
        Context.SaveChanges();
    }

    [TestMethod]
    public void GetAllAndGetByIdReturnStoredItemGroups()
    {
        CollectionAssert.AreEqual(new[] { 1, 2 }, Logic.GetAll().Select(itemGroup => itemGroup.Id).ToArray());
        Assert.AreEqual("Empty Group", Logic.GetById(2)?.Name);
        Assert.IsNull(Logic.GetById(999));
        Assert.IsNull(Logic.GetById(0));
        Assert.IsNull(Logic.GetById(-1));
    }

    [TestMethod]
    public void AddPersistsItemGroupAndSetsTimestamps()
    {
        ItemGroup itemGroup = TestData.CreateItemGroup(name: "New Group");

        Logic.Add(itemGroup);

        ItemGroup? stored = Logic.GetById(itemGroup.Id);
        Assert.IsNotNull(stored);
        Assert.AreEqual("New Group", stored.Name);
        Assert.IsTrue(stored.CreatedAt > TestData.UpdatedAt);
        Assert.AreEqual(stored.CreatedAt, stored.UpdatedAt);
    }

    [TestMethod]
    public void AddRejectsInvalidDuplicateAndNegativeItemGroups()
    {
        Assert.ThrowsException<ArgumentException>(() => Logic.Add(TestData.CreateItemGroup(name: " ")));
        Assert.ThrowsException<ArgumentNullException>(() => Logic.Add(null!));
        Assert.ThrowsException<InvalidOperationException>(() => Logic.Add(TestData.CreateItemGroup(1)));
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => Logic.Add(TestData.CreateItemGroup(-1)));
        Assert.AreEqual(2, Logic.GetAll().Count);
    }

    [TestMethod]
    public void UpdatePreservesCreationTimeAndReturnsFalseForMissingItemGroup()
    {
        ItemGroup updated = TestData.CreateItemGroup(1, "Renamed Group");
        updated.CreatedAt = DateTime.UtcNow;

        Assert.IsTrue(Logic.Update(1, updated));
        ItemGroup? stored = Logic.GetById(1);
        Assert.AreEqual("Renamed Group", stored?.Name);
        Assert.AreEqual(TestData.CreatedAt, stored?.CreatedAt);
        Assert.IsTrue(stored?.UpdatedAt > TestData.UpdatedAt);
        Assert.ThrowsException<ArgumentException>(() => Logic.Update(1, TestData.CreateItemGroup(name: "")));
        Assert.IsFalse(Logic.Update(999, TestData.CreateItemGroup()));
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => Logic.Update(0, TestData.CreateItemGroup()));
    }

    [TestMethod]
    public void RemoveDeletesEmptyItemGroupAndRejectsGroupWithItems()
    {
        Assert.ThrowsException<InvalidOperationException>(() => Logic.Remove(1));
        Assert.IsNotNull(Logic.GetById(1));

        Assert.IsTrue(Logic.Remove(2));
        Assert.IsNull(Logic.GetById(2));
        Assert.IsFalse(Logic.Remove(2));
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => Logic.Remove(0));
    }
}
