using CargoHUB.Datasource;
using CargoHUB.Models;
using Microsoft.EntityFrameworkCore;

namespace CargoHUB.Access;

public sealed class TransferItemAccess(CargoHubDbContext context)
{

    public TransferItem? GetById(int id)
    {
        return context.TransferItems.AsNoTracking().SingleOrDefault(row => row.Id == id);
    }

    public IReadOnlyList<TransferItem> GetItems(int id)
    {
        return context.Set<TransferItem>().AsNoTracking().Where(item => item.TransferId == id)
            .OrderBy(item => item.ItemId).ToList();
    }

    public void Add(TransferItem TransferItem)
    {
        context.TransferItems.Add(TransferItem);
        context.SaveChanges();
    }

    public void Update(int id, TransferItem TransferItem)
    {
        TransferItem? existing = context.TransferItems.SingleOrDefault(row => row.Id == id);
        if (existing is null)
            return;

        existing.TransferId = TransferItem.TransferId;
        existing.ItemId = TransferItem.ItemId;
        existing.Amount = TransferItem.Amount;
        context.SaveChanges();
    }
    public void Remove(int id)
    {
        TransferItem? existing = context.TransferItems.Find(id);
        if (existing is null)
            return;

        context.TransferItems.Remove(existing);
        context.SaveChanges();
    }
}
