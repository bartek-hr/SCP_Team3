using CargoHUB.Datasource;
using CargoHUB.Models;
using Microsoft.EntityFrameworkCore;

namespace CargoHUB.Access;

public sealed class ItemLineAccess(CargoHubDbContext context)
{
    public IReadOnlyList<ItemLine> GetAll() =>
        context.ItemLines.AsNoTracking().OrderBy(itemLine => itemLine.Id).ToList();

    public ItemLine? GetById(int id) =>
        context.ItemLines.AsNoTracking().SingleOrDefault(itemLine => itemLine.Id == id);

    public void Add(ItemLine itemLine)
    {
        context.ItemLines.Add(itemLine);
        context.SaveChanges();
    }

    public bool Update(int id, ItemLine itemLine)
    {
        ItemLine? existing = context.ItemLines.Find(id);
        if (existing is null)
        {
            return false;
        }

        existing.Name = itemLine.Name;
        existing.Description = itemLine.Description;
        existing.UpdatedAt = itemLine.UpdatedAt;
        context.SaveChanges();
        return true;
    }

    public bool Remove(int id)
    {
        ItemLine? itemLine = context.ItemLines.Find(id);
        if (itemLine is null)
        {
            return false;
        }

        context.ItemLines.Remove(itemLine);
        context.SaveChanges();
        return true;
    }
}
