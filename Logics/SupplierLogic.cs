using CargoHUB.Access;
using CargoHUB.Models;

namespace CargoHUB.Logics;

public sealed class SupplierLogic(SupplierAccess dataAccess)
{
    public IReadOnlyList<Supplier> GetAll() => dataAccess.GetAll();

    public Supplier? GetById(int id) => id > 0 ? dataAccess.GetById(id) : null;

    public void Add(Supplier supplier)
    {
        Validate(supplier);
        if (supplier.Id < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(supplier.Id));
        }

        if (supplier.Id > 0 && GetById(supplier.Id) is not null)
        {
            throw new InvalidOperationException($"A supplier with ID {supplier.Id} already exists.");
        }

        supplier.CreatedAt = DateTime.UtcNow;
        supplier.UpdatedAt = supplier.CreatedAt;
        dataAccess.Add(supplier);
    }

    public bool Update(int id, Supplier supplier)
    {
        if (id <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(id));
        }

        if (GetById(id) is null)
        {
            return false;
        }

        Validate(supplier);
        supplier.UpdatedAt = DateTime.UtcNow;
        return dataAccess.Update(id, supplier);
    }

    public bool Remove(int id)
    {
        if (id <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(id));
        }

        return dataAccess.Remove(id);
    }

    private static void Validate(Supplier supplier)
    {
        ArgumentNullException.ThrowIfNull(supplier);

        if (new[]
            {
                supplier.Code, supplier.Name, supplier.Address, supplier.City,
                supplier.ZipCode, supplier.Province, supplier.Country,
                supplier.ContactName, supplier.PhoneNumber, supplier.Reference
            }.Any(string.IsNullOrWhiteSpace))
        {
            throw new ArgumentException("All supplier fields are required.", nameof(supplier));
        }
    }
}
