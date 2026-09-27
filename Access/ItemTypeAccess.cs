using CargoHUB.Datasource;
using CargoHUB.Models;
using Microsoft.EntityFrameworkCore;

namespace CargoHUB.Access;

public sealed class ItemTypeAccess(CargoHubDbContext context)
{
    public IReadOnlyList<ItemType> GetAll() =>
        context.ItemTypes.AsNoTracking().OrderBy(itemType => itemType.Id).ToList();

    public ItemType? GetById(int id) =>
        context.ItemTypes.AsNoTracking().SingleOrDefault(itemType => itemType.Id == id);

    public void Add(ItemType itemType)
    {
        context.ItemTypes.Add(itemType);
        context.SaveChanges();
    }

    public bool Update(int id, ItemType itemType)
    {
        ItemType? existing = context.ItemTypes.Find(id);
        if (existing is null)
        {
            return false;
        }

        existing.Name = itemType.Name;
        existing.Description = itemType.Description;
        existing.UpdatedAt = itemType.UpdatedAt;
        context.SaveChanges();
        return true;
    }

    public bool Remove(int id)
    {
        ItemType? itemType = context.ItemTypes.Find(id);
        if (itemType is null)
        {
            return false;
        }

        context.ItemTypes.Remove(itemType);
        context.SaveChanges();
        return true;
    }
}
