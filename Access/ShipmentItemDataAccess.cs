using CargoHUB.Datasource;
using CargoHUB.Models;
using Microsoft.EntityFrameworkCore;

namespace CargoHUB.Access;

public sealed class ShipmentItemDataAccess(CargoHubDbContext context)
{
    public IReadOnlyList<ShipmentItem> GetByShipmentId(int shipmentId)
    {
        return context.Set<ShipmentItem>().AsNoTracking().Where(item => item.ShipmentId == shipmentId)
            .OrderBy(item => item.ItemId).ToList();
    }

    public bool ShipmentExists(int shipmentId)
    {
        return context.Shipments.Any(row => row.Id == shipmentId);
    }

    public void Replace(int shipmentId, IReadOnlyList<ShipmentItem> items, DateTime updatedAt)
    {
        Shipment? shipment = context.Shipments.Find(shipmentId);
        if (shipment is null)
        {
            return;
        }

        context.Set<ShipmentItem>().RemoveRange(context.Set<ShipmentItem>().Where(item => item.ShipmentId == shipmentId));
        context.Set<ShipmentItem>().AddRange(items.Select(item => new ShipmentItem
        {
            ShipmentId = shipmentId,
            ItemId = item.ItemId,
            Amount = item.Amount,
        }));
        shipment.UpdatedAt = updatedAt;
        context.SaveChanges();
    }
}
