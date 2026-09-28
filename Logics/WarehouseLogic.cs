using System.ComponentModel.DataAnnotations;
using CargoHUB.Access;
using CargoHUB.Models;

namespace CargoHUB.Logics;

public sealed class WarehouseLogic(WarehouseAccess warehouseAccess, LocationAccess locationAccess)
{
    public IReadOnlyList<Warehouse> GetAll() => warehouseAccess.GetAll();

    public Warehouse? GetById(int id) => id > 0 ? warehouseAccess.GetById(id) : null;

    public void Add(Warehouse warehouse)
    {
        Validate(warehouse);
        if (warehouse.Id < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(warehouse.Id));
        }

        if (warehouse.Id > 0 && GetById(warehouse.Id) is not null)
        {
            throw new InvalidOperationException($"A warehouse with ID {warehouse.Id} already exists.");
        }

        warehouse.CreatedAt = DateTime.UtcNow;
        warehouse.UpdatedAt = warehouse.CreatedAt;
        warehouseAccess.Add(warehouse);
    }

    public bool Update(int id, Warehouse warehouse)
    {
        if (id <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(id));
        }

        if (GetById(id) is null)
        {
            return false;
        }

        Validate(warehouse);
        warehouse.UpdatedAt = DateTime.UtcNow;
        return warehouseAccess.Update(id, warehouse);
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

        if (locationAccess.AnyInWarehouse(id))
        {
            throw new InvalidOperationException($"Warehouse {id} still has locations.");
        }

        return warehouseAccess.Remove(id);
    }

    private static void Validate(Warehouse warehouse)
    {
        ArgumentNullException.ThrowIfNull(warehouse);

        if (new[]
            {
                warehouse.Code, warehouse.Name, warehouse.Address, warehouse.City,
                warehouse.ZipCode, warehouse.Province, warehouse.Country,
                warehouse.ContactName, warehouse.ContactPhone, warehouse.ContactEmail
            }.Any(string.IsNullOrWhiteSpace))
        {
            throw new ArgumentException("All warehouse fields are required.", nameof(warehouse));
        }

        if (!new EmailAddressAttribute().IsValid(warehouse.ContactEmail))
        {
            throw new ArgumentException("ContactEmail must be a valid email address.", nameof(warehouse));
        }
    }
}
