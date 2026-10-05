using CargoHUB.Access;
using CargoHUB.Models;

namespace CargoHUB.Logics;

public sealed class LocationLogic(LocationAccess locationAccess, WarehouseAccess warehouseAccess, TransferAccess transferAccess)
{
    public IReadOnlyList<Location> GetAll() => locationAccess.GetAll();

    public Location? GetById(int id) => id > 0 ? locationAccess.GetById(id) : null;

    public IReadOnlyList<Location> GetByWarehouseId(int warehouseId) =>
        warehouseId > 0 ? locationAccess.GetByWarehouseId(warehouseId) : [];

    public void Add(Location location)
    {
        Validate(location);
        if (location.Id < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(location.Id));
        }

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

        return locationAccess.Remove(id);
    }

    private void Validate(Location location)
    {
        ArgumentNullException.ThrowIfNull(location);

        if (string.IsNullOrWhiteSpace(location.Code) || string.IsNullOrWhiteSpace(location.Name))
        {
            throw new ArgumentException("All location fields are required.", nameof(location));
        }

        if (location.WarehouseId <= 0 || warehouseAccess.GetById(location.WarehouseId) is null)
        {
            throw new ArgumentException($"Warehouse {location.WarehouseId} does not exist.", nameof(location));
        }
    }
}
