using System.ComponentModel.DataAnnotations;
using CargoHUB.Access;
using CargoHUB.Models;

namespace CargoHUB.Logics;

public sealed class WarehouseLogic(WarehouseAccess warehouseAccess, LocationAccess locationAccess, OrderDataAccess orderAccess)
{
    public IReadOnlyList<Warehouse> GetAll() => warehouseAccess.GetAll();

    public Warehouse? GetById(int id) => id > 0 ? warehouseAccess.GetById(id) : null;

    public void Add(Warehouse warehouse)
    {
        Validate(warehouse);
        Validation.NotNegative(warehouse.Id, "id");

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
        Validation.MatchesRoute(warehouse.Id, id);
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

        if (orderAccess.AnyForWarehouse(id))
        {
            throw new InvalidOperationException($"Warehouse {id} is still used by orders.");
        }

        return warehouseAccess.Remove(id);
    }

    private static void Validate(Warehouse warehouse)
    {
        ArgumentNullException.ThrowIfNull(warehouse);
        Validation.Required(warehouse.Code, "code");
        Validation.Required(warehouse.Name, "name");
        Validation.Required(warehouse.Address, "address");
        Validation.Required(warehouse.City, "city");
        Validation.Required(warehouse.ZipCode, "zip_code");
        Validation.Required(warehouse.Province, "province");
        Validation.Required(warehouse.Country, "country");
        Validation.Required(warehouse.ContactName, "contact_name");
        Validation.Required(warehouse.ContactPhone, "contact_phone");
        Validation.Required(warehouse.ContactEmail, "contact_email");

        if (!new EmailAddressAttribute().IsValid(warehouse.ContactEmail))
        {
            throw new ArgumentException("contact_email must be a valid email address.");
        }
    }
}
