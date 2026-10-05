using CargoHUB.Access;
using CargoHUB.Logics;
using CargoHUB.Models;
using CargoHUB.Tests.Infrastructure;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CargoHUB.Tests.Logics;

[TestClass]
public sealed class InventoryLogicTests : DatabaseTest
{
    [TestMethod]
    public void VoorraadUpsertBehoudtDeSleutelEnBerekentTotalen()
    {
        var logic = new InventoryLogic(new InventoryDataAccess(Context));
        logic.AddOrUpdate(new Inventory
        {
            ItemId = 1,
            LocationId = 1,
            QuantityOnHand = 10
        });
        logic.AddOrUpdate(new Inventory
        {
            ItemId = 1,
            LocationId = 1,
            QuantityOnHand = 20,
            QuantityAllocated = 3
        });

        Assert.AreEqual(1, logic.GetAll().Count);
        Assert.AreEqual(17L, logic.GetTotalsByItemId(1).TotalAvailable);
        Assert.AreEqual(0L, logic.GetTotalsByItemId(999).TotalAvailable);
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => logic.Remove(0, 1));
    }

}
