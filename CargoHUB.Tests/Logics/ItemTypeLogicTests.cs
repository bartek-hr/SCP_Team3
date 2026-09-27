using CargoHUB.Access;
using CargoHUB.Logics;
using CargoHUB.Models;
using CargoHUB.Tests.Infrastructure;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CargoHUB.Tests.Logics;

[TestClass]
public sealed class ItemTypeLogicTests : DatabaseTest
{
    private ItemTypeLogic Logic => new(new ItemTypeAccess(Context));

    protected override void SeedDatabase()
    {
        Context.ItemTypes.Add(CreateItemType(1));
        Context.SaveChanges();
    }

    [TestMethod]
    public void GetAllAndGetByIdReturnStoredItemTypes()
    {
        Assert.AreEqual(1, Logic.GetAll().Count);
        Assert.AreEqual("Single", Logic.GetById(1)?.Name);
        Assert.IsNull(Logic.GetById(999));
        Assert.IsNull(Logic.GetById(0));
    }

    [TestMethod]
    public void AddPersistsItemTypeAndSetsTimestamps()
    {
        ItemType itemType = CreateItemType();
        Logic.Add(itemType);

        ItemType? stored = Logic.GetById(itemType.Id);
        Assert.IsNotNull(stored);
        Assert.IsTrue(stored.CreatedAt > DateTime.UnixEpoch);
        Assert.AreEqual(stored.CreatedAt, stored.UpdatedAt);
    }

    [TestMethod]
    public void AddRejectsInvalidAndDuplicateItemTypes()
    {
        ItemType invalid = CreateItemType();
        invalid.Description = string.Empty;

        Assert.ThrowsException<ArgumentException>(() => Logic.Add(invalid));
        Assert.ThrowsException<InvalidOperationException>(() => Logic.Add(CreateItemType(1)));
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => Logic.Add(CreateItemType(-1)));
    }

    [TestMethod]
    public void UpdatePreservesCreationTimeAndReturnsFalseForMissingItemType()
    {
        DateTime createdAt = Logic.GetById(1)!.CreatedAt;
        ItemType updated = CreateItemType(1);
        updated.Name = "Updated type";

        Assert.IsTrue(Logic.Update(1, updated));
        Assert.AreEqual("Updated type", Logic.GetById(1)?.Name);
        Assert.AreEqual(createdAt, Logic.GetById(1)?.CreatedAt);
        Assert.IsTrue(Logic.GetById(1)?.UpdatedAt > createdAt);
        Assert.IsFalse(Logic.Update(999, CreateItemType()));
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => Logic.Update(0, CreateItemType()));
    }

    [TestMethod]
    public void RemoveDeletesItemTypeAndReturnsFalseForMissingItemType()
    {
        Assert.IsFalse(Logic.Remove(999));
        Assert.IsTrue(Logic.Remove(1));
        Assert.IsNull(Logic.GetById(1));
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => Logic.Remove(0));
    }

    private static ItemType CreateItemType(int id = 0) => new()
    {
        Id = id,
        Name = "Single",
        Description = "Packaging tier: Single",
        CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
        UpdatedAt = new DateTime(2024, 1, 2, 0, 0, 0, DateTimeKind.Utc),
    };
}
