using CargoHUB.Datasource;
using CargoHUB.Models;
using Microsoft.EntityFrameworkCore;

namespace CargoHUB.Access;

public sealed class ClientDataAccess
{
    private readonly CargoHubDbContext _context;

    public ClientDataAccess(CargoHubDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public IReadOnlyList<Client> GetAll() =>
        _context.Clients
            .AsNoTracking()
            .OrderBy(client => client.Id)
            .ToList();

    public Client? GetById(int id) =>
        _context.Clients
            .AsNoTracking()
            .SingleOrDefault(client => client.Id == id);

    public void Add(Client client)
    {
        _context.Clients.Add(client);
        _context.SaveChanges();
    }

    public void Update(int id, Client client)
    {
        Client? existingClient = _context.Clients.Find(id);
        if (existingClient is null)
        {
            return;
        }

        existingClient.Name = client.Name;
        existingClient.Address = client.Address;
        existingClient.City = client.City;
        existingClient.ZipCode = client.ZipCode;
        existingClient.Province = client.Province;
        existingClient.Country = client.Country;
        existingClient.ContactName = client.ContactName;
        existingClient.ContactPhone = client.ContactPhone;
        existingClient.ContactEmail = client.ContactEmail;
        existingClient.CreatedAt = client.CreatedAt;
        existingClient.UpdatedAt = client.UpdatedAt;

        _context.SaveChanges();
    }

    public void Remove(int id)
    {
        Client? client = _context.Clients.Find(id);
        if (client is null)
        {
            return;
        }

        _context.Clients.Remove(client);
        _context.SaveChanges();
    }
}
