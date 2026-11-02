using CargoHUB.Access;
using CargoHUB.Models;

namespace CargoHUB.Logics;

public sealed class LocationLogic(
    LocationAccess locationAccess,
    WarehouseAccess warehouseAccess,
    TransferAccess transferAccess,
    InventoryDataAccess inventoryAccess)
{
    public IReadOnlyList<Location> GetAll() => locationAccess.GetAll();

    public Location? GetById(int id) => id > 0 ? locationAccess.GetById(id) : null;

    public IReadOnlyList<Location> GetByWarehouseId(int warehouseId) =>
        warehouseId > 0 ? locationAccess.GetByWarehouseId(warehouseId) : [];

    public void Add(Location location)
    {
        Validate(location);
        Validation.NotNegative(location.Id, "id");

        if (location.Id > 0 && GetById(location.Id) is not null)
        {
            throw new InvalidOperationException($"A location with ID {location.Id} already exists.");
        }

        location.CreatedAt = DateTime.UtcNow;
        location.UpdatedAt = location.CreatedAt;
        locationAccess.Add(location);
    }

    public bool Update(int id, Location location)
    {
        if (id <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(id));
        }

        if (GetById(id) is null)
        {
            return false;
        }

        Validate(location);
        Validation.MatchesRoute(location.Id, id);
        location.UpdatedAt = DateTime.UtcNow;
        return locationAccess.Update(id, location);
    }

    public bool Remove(int id)
    {
        if (id <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(id));
        }

        if (GetById(id) is null)
        {
            return false;
        }

        if (transferAccess.AnyUsingLocation(id))
        {
            throw new InvalidOperationException($"Location {id} is still used by transfers.");
        }

        if (inventoryAccess.AnyAtLocation(id))
        {
            throw new InvalidOperationException($"Location {id} still has inventory.");
        }

        return locationAccess.Remove(id);
    }

    private void Validate(Location location)
    {
        ArgumentNullException.ThrowIfNull(location);
        Validation.Required(location.Code, "code");
        Validation.Required(location.Name, "name");
        Validation.Reference(location.WarehouseId, "warehouse_id", id => warehouseAccess.GetById(id) is not null);
    }
}
