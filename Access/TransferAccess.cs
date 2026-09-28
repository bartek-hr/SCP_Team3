using CargoHUB.Datasource;
using CargoHUB.Models;
using Microsoft.EntityFrameworkCore;

namespace CargoHUB.Access;

public sealed class TransferAccess(CargoHubDbContext context)
{
    public IReadOnlyList<Transfer> GetAll() =>
        context.Transfers
            .AsNoTracking()
            .Include(transfer => transfer.Items!.OrderBy(item => item.Id))
            .OrderBy(transfer => transfer.Id)
            .ToList();

    public Transfer? GetById(int id) =>
        context.Transfers
            .AsNoTracking()
            .Include(transfer => transfer.Items!.OrderBy(item => item.Id))
            .SingleOrDefault(transfer => transfer.Id == id);

    public bool AnyUsingLocation(int locationId) =>
        context.Transfers.Any(transfer => transfer.FromLocationId == locationId || transfer.ToLocationId == locationId);

    public void Add(Transfer transfer)
    {
        context.Transfers.Add(transfer);
        context.SaveChanges();
    }

    public bool Update(int id, Transfer transfer)
    {
        Transfer? existing = context.Transfers.Find(id);
        if (existing is null)
        {
            return false;
        }

        existing.Reference = transfer.Reference;
        existing.FromLocationId = transfer.FromLocationId;
        existing.ToLocationId = transfer.ToLocationId;
        existing.TransferStatus = transfer.TransferStatus;
        existing.UpdatedAt = transfer.UpdatedAt;

        if (transfer.Items is not null)
        {
            context.TransferItems.RemoveRange(context.TransferItems.Where(item => item.TransferId == id));
            context.TransferItems.AddRange(transfer.Items.Select(item => new TransferItem
            {
                TransferId = id,
                ItemId = item.ItemId,
                Amount = item.Amount,
            }));
        }

        context.SaveChanges();
        return true;
    }

    public bool Remove(int id)
    {
        // Loading the items lets EF delete them together with the transfer.
        Transfer? transfer = context.Transfers
            .Include(stored => stored.Items)
            .SingleOrDefault(stored => stored.Id == id);
        if (transfer is null)
        {
            return false;
        }

        context.Transfers.Remove(transfer);
        context.SaveChanges();
        return true;
    }
}
