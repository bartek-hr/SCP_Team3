using CargoHUB.Access;
using CargoHUB.Models;

namespace CargoHUB.Logics;

public sealed class ItemGroupLogic(ItemGroupAccess itemGroupAccess, ItemAccess itemAccess)
{

    // Python: get_item_groups()
    public IReadOnlyList<ItemGroup> GetAll() => itemGroupAccess.GetAll();

    // Python: get_item_group(item_group_id)
    public ItemGroup? GetById(int id) => id > 0 ? itemGroupAccess.GetById(id) : null;

    // Python: add_item_group(item_group)
    public void Add(ItemGroup itemGroup)
    {
        Validate(itemGroup);
        if (itemGroup.Id < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(itemGroup.Id));
        }

        if (itemGroup.Id > 0 && GetById(itemGroup.Id) is not null)
        {
            throw new InvalidOperationException($"An item group with ID {itemGroup.Id} already exists.");
        }

        itemGroup.CreatedAt = DateTime.UtcNow;
        itemGroup.UpdatedAt = itemGroup.CreatedAt;
        itemGroupAccess.Add(itemGroup);
    }

    // Python: update_item_group(item_group_id, item_group)
    public bool Update(int id, ItemGroup itemGroup)
    {
        if (id <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(id));
        }

        if (GetById(id) is null)
        {
            return false;
        }

        Validate(itemGroup);
        itemGroup.UpdatedAt = DateTime.UtcNow;
        return itemGroupAccess.Update(id, itemGroup);
    }

    // Python: remove_item_group(item_group_id)
    public bool Remove(int id)
    {
        if (id <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(id));
        }

        if (GetById(id) is null)
        {
            return false;
        }

        // New in C#: Python removed the group even when items still pointed to it.
        if (itemAccess.AnyInItemGroup(id))
        {
            throw new InvalidOperationException($"Item group {id} still has items.");
        }

        return itemGroupAccess.Remove(id);
    }

    // New in C#: the Python version did no validation.
    private static void Validate(ItemGroup itemGroup)
    {
        ArgumentNullException.ThrowIfNull(itemGroup);

        if (string.IsNullOrWhiteSpace(itemGroup.Name))
        {
            throw new ArgumentException("Name is required.", nameof(itemGroup));
        }
    }
}
