using CargoHUB.Datasource;
using CargoHUB.Models;
using Microsoft.EntityFrameworkCore;

namespace CargoHUB.Access;

public sealed class WarehouseAccess(CargoHubDbContext context)
{
    public IReadOnlyList<Warehouse> GetAll() =>
        context.Warehouses.AsNoTracking().OrderBy(warehouse => warehouse.Id).ToList();

    public Warehouse? GetById(int id) =>
        context.Warehouses.AsNoTracking().SingleOrDefault(warehouse => warehouse.Id == id);

    public void Add(Warehouse warehouse)
    {
        context.Warehouses.Add(warehouse);
        context.SaveChanges();
    }

    public bool Update(int id, Warehouse warehouse)
    {
        Warehouse? existing = context.Warehouses.Find(id);
        if (existing is null)
        {
            return false;
        }

        existing.Code = warehouse.Code;
        existing.Name = warehouse.Name;
        existing.Address = warehouse.Address;
        existing.City = warehouse.City;
        existing.ZipCode = warehouse.ZipCode;
        existing.Province = warehouse.Province;
        existing.Country = warehouse.Country;
        existing.ContactName = warehouse.ContactName;
        existing.ContactPhone = warehouse.ContactPhone;
        existing.ContactEmail = warehouse.ContactEmail;
        existing.UpdatedAt = warehouse.UpdatedAt;
        context.SaveChanges();
        return true;
    }

    public bool Remove(int id)
    {
        Warehouse? warehouse = context.Warehouses.Find(id);
        if (warehouse is null)
        {
            return false;
        }

        context.Warehouses.Remove(warehouse);
        context.SaveChanges();
        return true;
    }
}
