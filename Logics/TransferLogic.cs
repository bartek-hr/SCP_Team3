using CargoHUB.Access;
using CargoHUB.Models;

namespace CargoHUB.Logics;

public sealed class TransferLogic(TransferAccess transferAccess, LocationAccess locationAccess, ItemAccess itemAccess)
{
    public IReadOnlyList<Transfer> GetAll() => transferAccess.GetAll();

    public Transfer? GetById(int id) => id > 0 ? transferAccess.GetById(id) : null;

    public IReadOnlyList<TransferItem>? GetItems(int id) => GetById(id)?.Items;

    public void Add(Transfer transfer)
    {
        Validate(transfer);
        Validation.NotNegative(transfer.Id, "id");
        if (transfer.Id > 0 && GetById(transfer.Id) is not null)
        {
            throw new InvalidOperationException($"A transfer with ID {transfer.Id} already exists.");
        }

        // A new transfer always starts as scheduled, whatever the client sends.
        transfer.TransferStatus = TransferStatus.Scheduled;
        transfer.Items ??= [];
        transfer.CreatedAt = DateTime.UtcNow;
        transfer.UpdatedAt = transfer.CreatedAt;
        transferAccess.Add(transfer);
    }

    public bool Update(int id, Transfer transfer)
    {
        if (id <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(id));
        }

        Transfer? existing = GetById(id);
        if (existing is null)
        {
            return false;
        }

        Validate(transfer);
        Validation.MatchesRoute(transfer.Id, id);
        if (existing.TransferStatus == TransferStatus.Processed)
        {
            // The stock has already moved, so changing the transfer would no longer match the inventory.
            throw new InvalidOperationException($"Transfer {id} is already processed and cannot be changed.");
        }

        // The status only changes by committing the transfer, so a client cannot mark it processed.
        transfer.TransferStatus = existing.TransferStatus;
        transfer.UpdatedAt = DateTime.UtcNow;
        return transferAccess.Update(id, transfer);
    }

    public bool Remove(int id)
    {
        if (id <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(id));
        }

        return transferAccess.Remove(id);
    }

    private void Validate(Transfer transfer)
    {
        ArgumentNullException.ThrowIfNull(transfer);
        Validation.Required(transfer.Reference, "reference");
        Validation.Reference(transfer.FromLocationId, "from_location_id", id => locationAccess.GetById(id) is not null);
        Validation.Reference(transfer.ToLocationId, "to_location_id", id => locationAccess.GetById(id) is not null);
        if (transfer.FromLocationId == transfer.ToLocationId)
        {
            throw new ArgumentException("from_location_id and to_location_id must be different locations.");
        }

        // Null means the items were not sent; an update then keeps the stored items.
        if (transfer.Items is not null)
        {
            Validation.Items(transfer.Items, item => item.ItemId, item => item.Amount, id => itemAccess.GetById(id) is not null);
        }
    }
}
