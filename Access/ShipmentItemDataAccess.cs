using CargoHUB.Datasource;
using CargoHUB.Models;
using Microsoft.EntityFrameworkCore;

namespace CargoHUB.Access;

public sealed class ShipmentItemDataAccess(CargoHubDbContext context)
{

    public ShipmentItem? GetById(int id)
    {
        return context.ShipmentItems.AsNoTracking().SingleOrDefault(row => row.Id == id);
    }

    public IReadOnlyList<ShipmentItem> GetItems(int id)
    {
        return context.Set<ShipmentItem>().AsNoTracking().Where(item => item.ShipmentId == id)
            .OrderBy(item => item.ItemId).ToList();
    }

    public void Add(ShipmentItem ShipmentItem)
    {
        context.ShipmentItems.Add(ShipmentItem);
        context.SaveChanges();
    }

    public void Update(int id, ShipmentItem ShipmentItem)
    {
        ShipmentItem? existing = context.ShipmentItems.SingleOrDefault(row => row.Id == id);
        if (existing is null)
            return;

        existing.ShipmentId = ShipmentItem.ShipmentId;
        existing.ItemId = ShipmentItem.ItemId;
        existing.Amount = ShipmentItem.Amount;
        context.SaveChanges();
    }
    public void Remove(int id)
    {
        ShipmentItem? existing = context.ShipmentItems.Find(id);
        if (existing is null)
            return;

        context.ShipmentItems.Remove(existing);
        context.SaveChanges();
    }
}
