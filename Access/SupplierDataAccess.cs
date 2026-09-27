using CargoHUB.Datasource;
using CargoHUB.Models;
using Microsoft.EntityFrameworkCore;

namespace CargoHUB.Access;

public sealed class SupplierDataAccess(CargoHubDbContext context)
{
    public IReadOnlyList<Supplier> GetAll() =>
        context.Suppliers.AsNoTracking().OrderBy(supplier => supplier.Id).ToList();

    public Supplier? GetById(int id) =>
        context.Suppliers.AsNoTracking().SingleOrDefault(supplier => supplier.Id == id);

    public void Add(Supplier supplier)
    {
        context.Suppliers.Add(supplier);
        context.SaveChanges();
    }

    public bool Update(int id, Supplier supplier)
    {
        Supplier? existing = context.Suppliers.Find(id);
        if (existing is null)
        {
            return false;
        }

        existing.Code = supplier.Code;
        existing.Name = supplier.Name;
        existing.Address = supplier.Address;
        existing.City = supplier.City;
        existing.ZipCode = supplier.ZipCode;
        existing.Province = supplier.Province;
        existing.Country = supplier.Country;
        existing.ContactName = supplier.ContactName;
        existing.PhoneNumber = supplier.PhoneNumber;
        existing.Reference = supplier.Reference;
        existing.UpdatedAt = supplier.UpdatedAt;
        context.SaveChanges();
        return true;
    }

    public bool Remove(int id)
    {
        Supplier? supplier = context.Suppliers.Find(id);
        if (supplier is null)
        {
            return false;
        }

        context.Suppliers.Remove(supplier);
        context.SaveChanges();
        return true;
    }
}
