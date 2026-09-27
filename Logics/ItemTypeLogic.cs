using CargoHUB.Access;
using CargoHUB.Models;

namespace CargoHUB.Logics;

public sealed class ItemTypeLogic(ItemTypeAccess dataAccess)
{
    public IReadOnlyList<ItemType> GetAll() => dataAccess.GetAll();

    public ItemType? GetById(int id) => id > 0 ? dataAccess.GetById(id) : null;

    public void Add(ItemType itemType)
    {
        Validate(itemType);
        if (itemType.Id < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(itemType.Id));
        }

        if (itemType.Id > 0 && GetById(itemType.Id) is not null)
        {
            throw new InvalidOperationException($"An item type with ID {itemType.Id} already exists.");
        }

        itemType.CreatedAt = DateTime.UtcNow;
        itemType.UpdatedAt = itemType.CreatedAt;
        dataAccess.Add(itemType);
    }

    public bool Update(int id, ItemType itemType)
    {
        if (id <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(id));
        }

        if (GetById(id) is null)
        {
            return false;
        }

        Validate(itemType);
        itemType.UpdatedAt = DateTime.UtcNow;
        return dataAccess.Update(id, itemType);
    }

    public bool Remove(int id)
    {
        if (id <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(id));
        }

        return dataAccess.Remove(id);
    }

    private static void Validate(ItemType itemType)
    {
        ArgumentNullException.ThrowIfNull(itemType);

        if (string.IsNullOrWhiteSpace(itemType.Name) || string.IsNullOrWhiteSpace(itemType.Description))
        {
            throw new ArgumentException("All item type fields are required.", nameof(itemType));
        }
    }
}
