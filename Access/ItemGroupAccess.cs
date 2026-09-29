using CargoHUB.Datasource;
using CargoHUB.Models;
using Microsoft.EntityFrameworkCore;

namespace CargoHUB.Access;

public sealed class ItemGroupAccess(CargoHubDbContext context)
{
    public IReadOnlyList<ItemGroup> GetAll() =>
        context.ItemGroups.AsNoTracking().OrderBy(itemGroup => itemGroup.Id).ToList();

    public ItemGroup? GetById(int id) =>
        context.ItemGroups.AsNoTracking().SingleOrDefault(itemGroup => itemGroup.Id == id);

    public void Add(ItemGroup itemGroup)
    {
        context.ItemGroups.Add(itemGroup);
        context.SaveChanges();
    }

    public bool Update(int id, ItemGroup itemGroup)
    {
        ItemGroup? existing = context.ItemGroups.Find(id);
        if (existing is null)
        {
            return false;
        }

        existing.Name = itemGroup.Name;
        existing.Description = itemGroup.Description;
        existing.UpdatedAt = itemGroup.UpdatedAt;
        context.SaveChanges();
        return true;
    }

    public bool Remove(int id)
    {
        ItemGroup? itemGroup = context.ItemGroups.Find(id);
        if (itemGroup is null)
        {
            return false;
        }

        context.ItemGroups.Remove(itemGroup);
        context.SaveChanges();
        return true;
    }
}
