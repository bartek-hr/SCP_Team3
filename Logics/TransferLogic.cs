using CargoHUB.Access;
using CargoHUB.Models;

namespace CargoHUB.Logics;

public sealed class TransferLogic(TransferAccess transferAccess, LocationAccess locationAccess)
{
    public IReadOnlyList<Transfer> GetAll() => transferAccess.GetAll();

    public Transfer? GetById(int id) => id > 0 ? transferAccess.GetById(id) : null;

    public IReadOnlyList<TransferItem>? GetItems(int id) => GetById(id)?.Items;

    public void Add(Transfer transfer)
    {
        Validate(transfer);
        if (transfer.Id < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(transfer.Id));
        }

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

        if (string.IsNullOrWhiteSpace(transfer.Reference))
        {
            throw new ArgumentException("Reference is required.", nameof(transfer));
        }

        if (transfer.FromLocationId == transfer.ToLocationId)
        {
            throw new ArgumentException("A transfer must move items between two different locations.", nameof(transfer));
        }

        foreach (int locationId in new[] { transfer.FromLocationId, transfer.ToLocationId })
        {
            if (locationId <= 0 || locationAccess.GetById(locationId) is null)
            {
                throw new ArgumentException($"Location {locationId} does not exist.", nameof(transfer));
            }
        }

        if (transfer.Items is not null && transfer.Items.Any(item => item is null || item.ItemId <= 0 || item.Amount <= 0))
        {
            throw new ArgumentException("Every transfer item needs a positive item_id and amount.", nameof(transfer));
        }
    }
}
