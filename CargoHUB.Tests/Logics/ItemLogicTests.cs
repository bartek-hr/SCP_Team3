using CargoHUB.Access;
using CargoHUB.Logics;
using CargoHUB.Models;
using CargoHUB.Tests.Infrastructure;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CargoHUB.Tests.Logics;

[TestClass]
public sealed class ItemLogicTests : DatabaseTest
{
    private ItemLogic Logic => new(new ItemAccess(Context), new ItemLineAccess(Context), new ItemGroupAccess(Context));

    protected override void SeedDatabase()
    {
        Context.ItemLines.AddRange(TestData.CreateItemLine(1), TestData.CreateItemLine(2));
        Context.ItemGroups.AddRange(TestData.CreateItemGroup(1), TestData.CreateItemGroup(2));

        Item second = TestData.CreateCatalogItem(2, itemLineId: 2, itemGroupId: 1, code: "TST-ITEM-2");
        second.ItemTypeId = 2;
        second.SupplierId = 2;
        Context.Items.AddRange(
            TestData.CreateCatalogItem(1, itemLineId: 1, itemGroupId: 1),
            second,
            TestData.CreateCatalogItem(3, itemLineId: 1, itemGroupId: 2, code: "TST-ITEM-3"));
        Context.SaveChanges();
    }

    [TestMethod]
    public void GetAllAndGetByIdReturnStoredItems()
    {
        CollectionAssert.AreEqual(new[] { 1, 2, 3 }, Logic.GetAll().Select(item => item.Id).ToArray());
        Item? item = Logic.GetById(2);
        Assert.AreEqual("TST-ITEM-2", item?.Code);
        Assert.AreEqual("0012345678905", item?.Barcode);
        Assert.AreEqual(1.5m, item?.UnitWeight);
        Assert.IsNull(Logic.GetById(999));
        Assert.IsNull(Logic.GetById(0));
        Assert.IsNull(Logic.GetById(-1));
    }

    [TestMethod]
    public void RelationLookupsReturnMatchingItems()
    {
        CollectionAssert.AreEqual(new[] { 1, 3 }, Logic.GetIdsForItemLine(1).ToArray());
        CollectionAssert.AreEqual(new[] { 1, 2 }, Logic.GetIdsForItemGroup(1).ToArray());
        CollectionAssert.AreEqual(new[] { 1, 3 }, Logic.GetIdsForItemType(1).ToArray());
        CollectionAssert.AreEqual(new[] { 2 }, Logic.GetForSupplier(2).Select(item => item.Id).ToArray());

        Assert.AreEqual(0, Logic.GetIdsForItemLine(999).Count);
        Assert.AreEqual(0, Logic.GetIdsForItemGroup(999).Count);
        Assert.AreEqual(0, Logic.GetIdsForItemType(999).Count);
        Assert.AreEqual(0, Logic.GetForSupplier(999).Count);
    }

    [TestMethod]
    public void AddPersistsItemAndSetsTimestamps()
    {
        Item item = TestData.CreateCatalogItem(code: "NEW-ITEM");

        Logic.Add(item);

        Item? stored = Logic.GetById(item.Id);
        Assert.IsNotNull(stored);
        Assert.AreEqual("NEW-ITEM", stored.Code);
        Assert.IsTrue(stored.CreatedAt > TestData.UpdatedAt);
        Assert.AreEqual(stored.CreatedAt, stored.UpdatedAt);
    }

    [TestMethod]
    public void AddRejectsInvalidItems()
    {
        Item missingCode = TestData.CreateCatalogItem();
        missingCode.Code = " ";
        Item negativeWeight = TestData.CreateCatalogItem();
        negativeWeight.UnitWeight = -1;
        Item negativeQuantity = TestData.CreateCatalogItem();
        negativeQuantity.CaseSize = -1;

        Assert.ThrowsException<ArgumentNullException>(() => Logic.Add(null!));
        Assert.ThrowsException<ArgumentException>(() => Logic.Add(missingCode));
        Assert.ThrowsException<ArgumentException>(() => Logic.Add(negativeWeight));
        Assert.ThrowsException<ArgumentException>(() => Logic.Add(negativeQuantity));
        Assert.ThrowsException<ArgumentException>(() => Logic.Add(TestData.CreateCatalogItem(itemLineId: 999)));
        Assert.ThrowsException<ArgumentException>(() => Logic.Add(TestData.CreateCatalogItem(itemGroupId: 999)));
        Assert.AreEqual(3, Logic.GetAll().Count);
    }

    [TestMethod]
    public void AddRejectsDuplicateAndNegativeIds()
    {
        Assert.ThrowsException<InvalidOperationException>(() => Logic.Add(TestData.CreateCatalogItem(1)));
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => Logic.Add(TestData.CreateCatalogItem(-1)));
        Assert.AreEqual(3, Logic.GetAll().Count);
    }

    [TestMethod]
    public void UpdateCopiesFieldsPreservesCreationTimeAndReturnsFalseForMissingItem()
    {
        Item updated = TestData.CreateCatalogItem(1, itemLineId: 2, itemGroupId: 2, code: "UPDATED");
        updated.UnitWeight = 9.25m;
        updated.CreatedAt = DateTime.UtcNow;

        Assert.IsTrue(Logic.Update(1, updated));
        Item? stored = Logic.GetById(1);
        Assert.AreEqual("UPDATED", stored?.Code);
        Assert.AreEqual(9.25m, stored?.UnitWeight);
        Assert.AreEqual(2, stored?.ItemLineId);
        Assert.AreEqual(2, stored?.ItemGroupId);
        Assert.AreEqual(TestData.CreatedAt, stored?.CreatedAt);
        Assert.IsTrue(stored?.UpdatedAt > TestData.UpdatedAt);
        Assert.ThrowsException<ArgumentException>(() => Logic.Update(1, TestData.CreateCatalogItem(itemLineId: 999)));
        Assert.IsFalse(Logic.Update(999, TestData.CreateCatalogItem()));
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => Logic.Update(0, TestData.CreateCatalogItem()));
    }

    [TestMethod]
    public void RemoveDeletesItemAndReturnsFalseForMissingItem()
    {
        Assert.IsTrue(Logic.Remove(1));
        Assert.IsNull(Logic.GetById(1));
        Assert.IsFalse(Logic.Remove(1));
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => Logic.Remove(0));
    }
}
