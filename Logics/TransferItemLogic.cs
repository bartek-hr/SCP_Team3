using CargoHUB.Access;
using CargoHUB.Models;

namespace CargoHUB.Logics;

public sealed class TransferItemLogic(TransferItemAccess dataAccess)
{

    public TransferItem? GetById(int id)
    {
        if (id <= 0)
        {
            return null;
        }

        return dataAccess.GetById(id);
    }

    public IReadOnlyList<TransferItem> GetItems(int TransferId)
    {
        if (TransferId <= 0)
        {
            return [];
        }

        return dataAccess.GetItems(TransferId);
    }


    public void Add(TransferItem Transferitem)
    {
        ValidateItem(Transferitem);
        if (Transferitem.Id > 0 && GetById(Transferitem.Id) is not null)
        {
            throw new InvalidOperationException($"A Transfer with ID {Transferitem.Id} already exists.");
        }
        dataAccess.Add(Transferitem);
    }

    public void Update(int id, TransferItem Transferitem)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(id);
        ValidateItem(Transferitem);
        if (Transferitem.Id != 0 && Transferitem.Id != id)
        {
            throw new ArgumentException("The Transfer ID must match the requested ID.", nameof(Transferitem));
        }

        TransferItem? existing = GetById(id);
        if (existing is null)
        {
            return;
        }

        dataAccess.Update(id, Transferitem);
    }

    public void Remove(int id)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(id);
        dataAccess.Remove(id);
    }

    public static void ValidateItem(TransferItem item)
    {
        ArgumentNullException.ThrowIfNull(item);
        ArgumentNullException.ThrowIfNull(item);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(item.ItemId);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(item.Amount);
    }

    public static void ValidateItems(IReadOnlyList<TransferItem> items)
    {
        ArgumentNullException.ThrowIfNull(items);
        var itemIds = new HashSet<int>();
        foreach (TransferItem item in items)
        {
            ArgumentNullException.ThrowIfNull(item);
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(item.ItemId);
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(item.Amount);
            if (!itemIds.Add(item.ItemId))
                throw new ArgumentException("Each item may appear only once.", nameof(items));
        }
    }
}
