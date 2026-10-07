using CargoHUB.Access;
using CargoHUB.Models;

namespace CargoHUB.Logics;

public sealed class ItemLogic(ItemAccess itemAccess, ItemLineAccess itemLineAccess, ItemGroupAccess itemGroupAccess)
{

    // Python: get_items()
    public IReadOnlyList<Item> GetAll() => itemAccess.GetAll();

    // Python: get_item(item_id)
    public Item? GetById(int id) => id > 0 ? itemAccess.GetById(id) : null;

    // Python: get_items_for_item_line(item_line_id) — returns item IDs, like Python did.
    public IReadOnlyList<int> GetIdsForItemLine(int itemLineId) => itemAccess.GetIdsForItemLine(itemLineId);

    // Python: get_items_for_item_group(item_group_id) — returns item IDs, like Python did.
    public IReadOnlyList<int> GetIdsForItemGroup(int itemGroupId) => itemAccess.GetIdsForItemGroup(itemGroupId);

    // Python: get_items_for_item_type(item_type_id) — returns item IDs, like Python did.
    public IReadOnlyList<int> GetIdsForItemType(int itemTypeId) => itemAccess.GetIdsForItemType(itemTypeId);

    // Python: get_items_for_supplier(supplier_id) — returns full items, like Python did.
    public IReadOnlyList<Item> GetForSupplier(int supplierId) => itemAccess.GetForSupplier(supplierId);

    // Python: add_item(item)
    public void Add(Item item)
    {
        Validate(item);
        if (item.Id < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(item.Id));
        }

        if (item.Id > 0 && GetById(item.Id) is not null)
        {
            throw new InvalidOperationException($"An item with ID {item.Id} already exists.");
        }

        item.CreatedAt = DateTime.UtcNow;
        item.UpdatedAt = item.CreatedAt;
        itemAccess.Add(item);
    }

    // Python: update_item(item_id, item)
    public bool Update(int id, Item item)
    {
        if (id <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(id));
        }

        if (GetById(id) is null)
        {
            return false;
        }

        Validate(item);
        item.UpdatedAt = DateTime.UtcNow;
        return itemAccess.Update(id, item);
    }

    // Python: remove_item(item_id)
    public bool Remove(int id)
    {
        if (id <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(id));
        }

        return itemAccess.Remove(id);
    }

    // New in C#: the Python version did no validation.
    private void Validate(Item item)
    {
        ArgumentNullException.ThrowIfNull(item);

        if (string.IsNullOrWhiteSpace(item.Code) || string.IsNullOrWhiteSpace(item.Description))
        {
            throw new ArgumentException("Code and Description are required.", nameof(item));
        }

        if (item.UnitWeight < 0 || item.MinPurchaseQty < 0 || item.CaseSize < 0 || item.OrderMultiple < 0)
        {
            throw new ArgumentException("Weights and quantities cannot be negative.", nameof(item));
        }

        if (itemLineAccess.GetById(item.ItemLineId) is null)
        {
            throw new ArgumentException($"Item line {item.ItemLineId} does not exist.", nameof(item));
        }

        if (itemGroupAccess.GetById(item.ItemGroupId) is null)
        {
            throw new ArgumentException($"Item group {item.ItemGroupId} does not exist.", nameof(item));
        }
    }
}
