using CargoHUB.Access;
using CargoHUB.Models;

namespace CargoHUB.Logics;

public sealed class TransferItemLogic(TransferItemAccess dataAccess, TransferAccess transferAccess, ItemAccess itemAccess)
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
        Validate(Transferitem);
        if (Transferitem.Id > 0 && GetById(Transferitem.Id) is not null)
        {
            throw new InvalidOperationException($"A transfer item with ID {Transferitem.Id} already exists.");
        }

        EnsureTransferIsScheduled(Transferitem.TransferId);
        EnsureItemNotInTransfer(Transferitem, 0);
        dataAccess.Add(Transferitem);
    }

    public void Update(int id, TransferItem Transferitem)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(id);
        Validate(Transferitem);
        Validation.MatchesRoute(Transferitem.Id, id);

        TransferItem? existing = GetById(id);
        if (existing is null)
        {
            return;
        }

        // Both the current and the new transfer must still be open.
        EnsureTransferIsScheduled(existing.TransferId);
        EnsureTransferIsScheduled(Transferitem.TransferId);
        EnsureItemNotInTransfer(Transferitem, id);
        dataAccess.Update(id, Transferitem);
    }

    public void Remove(int id)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(id);
        if (GetById(id) is { } existing)
        {
            EnsureTransferIsScheduled(existing.TransferId);
        }

        dataAccess.Remove(id);
    }

    private void Validate(TransferItem transferitem)
    {
        ArgumentNullException.ThrowIfNull(transferitem);
        // transfer_id is not read from JSON (see TransferItem), so a request body always arrives with 0 here.
        Validation.Reference(transferitem.TransferId, "transfer_id", id => transferAccess.GetById(id) is not null);
        Validation.Reference(transferitem.ItemId, "item_id", id => itemAccess.GetById(id) is not null);
        Validation.Positive(transferitem.Amount, "amount");
    }

    // A processed transfer has moved its stock, so its items are final.
    private void EnsureTransferIsScheduled(int transferId)
    {
        if (transferAccess.GetById(transferId)?.TransferStatus == TransferStatus.Processed)
        {
            throw new InvalidOperationException($"Transfer {transferId} is already processed and cannot be changed.");
        }
    }

    // A transfer lists each item once, like the items of PUT /transfers/{id}.
    private void EnsureItemNotInTransfer(TransferItem transferitem, int ownId)
    {
        if (dataAccess.GetItems(transferitem.TransferId).Any(row => row.ItemId == transferitem.ItemId && row.Id != ownId))
        {
            throw new InvalidOperationException($"Transfer {transferitem.TransferId} already contains item_id {transferitem.ItemId}.");
        }
    }
}
