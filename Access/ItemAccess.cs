using CargoHUB.Datasource;
using CargoHUB.Models;
using Microsoft.EntityFrameworkCore;

namespace CargoHUB.Access;

public sealed class ItemAccess(CargoHubDbContext context)
{
    public IReadOnlyList<Item> GetAll() =>
        context.Items.AsNoTracking().OrderBy(item => item.Id).ToList();

    public Item? GetById(int id) =>
        context.Items.AsNoTracking().SingleOrDefault(item => item.Id == id);

    public IReadOnlyList<int> GetIdsForItemLine(int itemLineId) =>
        context.Items.AsNoTracking().Where(item => item.ItemLineId == itemLineId)
            .OrderBy(item => item.Id).Select(item => item.Id).ToList();

    public IReadOnlyList<int> GetIdsForItemGroup(int itemGroupId) =>
        context.Items.AsNoTracking().Where(item => item.ItemGroupId == itemGroupId)
            .OrderBy(item => item.Id).Select(item => item.Id).ToList();

    public IReadOnlyList<int> GetIdsForItemType(int itemTypeId) =>
        context.Items.AsNoTracking().Where(item => item.ItemTypeId == itemTypeId)
            .OrderBy(item => item.Id).Select(item => item.Id).ToList();

    public IReadOnlyList<Item> GetForSupplier(int supplierId) =>
        context.Items.AsNoTracking().Where(item => item.SupplierId == supplierId)
            .OrderBy(item => item.Id).ToList();

    public bool AnyInItemLine(int itemLineId) =>
        context.Items.Any(item => item.ItemLineId == itemLineId);

    public bool AnyInItemGroup(int itemGroupId) =>
        context.Items.Any(item => item.ItemGroupId == itemGroupId);

    public void Add(Item item)
    {
        context.Items.Add(item);
        context.SaveChanges();
    }

    public bool Update(int id, Item item)
    {
        Item? existing = context.Items.Find(id);
        if (existing is null)
        {
            return false;
        }

        existing.Code = item.Code;
        existing.Description = item.Description;
        existing.Barcode = item.Barcode;
        existing.ModelNumber = item.ModelNumber;
        existing.CommodityCode = item.CommodityCode;
        existing.UnitWeight = item.UnitWeight;
        existing.ItemLineId = item.ItemLineId;
        existing.ItemGroupId = item.ItemGroupId;
        existing.ItemTypeId = item.ItemTypeId;
        existing.MinPurchaseQty = item.MinPurchaseQty;
        existing.CaseSize = item.CaseSize;
        existing.PackagingType = item.PackagingType;
        existing.OrderMultiple = item.OrderMultiple;
        existing.SupplierId = item.SupplierId;
        existing.SupplierSku = item.SupplierSku;
        existing.UpdatedAt = item.UpdatedAt;
        context.SaveChanges();
        return true;
    }

    public bool Remove(int id)
    {
        Item? item = context.Items.Find(id);
        if (item is null)
        {
            return false;
        }

        context.Items.Remove(item);
        context.SaveChanges();
        return true;
    }
}
