using CargoHUB.Datasource;
using CargoHUB.Models;
using Microsoft.EntityFrameworkCore;

namespace CargoHUB.Access;

public sealed class LocationAccess(CargoHubDbContext context)
{
    public IReadOnlyList<Location> GetAll() =>
        context.Locations.AsNoTracking().OrderBy(location => location.Id).ToList();

    public Location? GetById(int id) =>
        context.Locations.AsNoTracking().SingleOrDefault(location => location.Id == id);

    public IReadOnlyList<Location> GetByWarehouseId(int warehouseId) =>
        context.Locations
            .AsNoTracking()
            .Where(location => location.WarehouseId == warehouseId)
            .OrderBy(location => location.Id)
            .ToList();

    public bool AnyInWarehouse(int warehouseId) =>
        context.Locations.Any(location => location.WarehouseId == warehouseId);

    public void Add(Location location)
    {
        context.Locations.Add(location);
        context.SaveChanges();
    }

    public bool Update(int id, Location location)
    {
        Location? existing = context.Locations.Find(id);
        if (existing is null)
        {
            return false;
        }

        existing.WarehouseId = location.WarehouseId;
        existing.Code = location.Code;
        existing.Name = location.Name;
        existing.UpdatedAt = location.UpdatedAt;
        context.SaveChanges();
        return true;
    }

    public bool Remove(int id)
    {
        Location? location = context.Locations.Find(id);
        if (location is null)
        {
            return false;
        }

        context.Locations.Remove(location);
        context.SaveChanges();
        return true;
    }
}
