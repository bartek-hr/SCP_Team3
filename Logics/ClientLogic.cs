using System.ComponentModel.DataAnnotations;
using CargoHUB.Access;
using CargoHUB.Models;

namespace CargoHUB.Logics;

public sealed class ClientLogic
{
    private readonly ClientDataAccess _dataAccess;

    public ClientLogic(ClientDataAccess dataAccess)
    {
        _dataAccess = dataAccess ?? throw new ArgumentNullException(nameof(dataAccess));
    }

    public IReadOnlyList<Client> GetAll() => _dataAccess.GetAll();

    public Client? GetById(int id) => id > 0 ? _dataAccess.GetById(id) : null;

    public void Add(Client client)
    {
        Validate(client);

        if (client.Id < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(client.Id));
        }

        if (client.Id > 0 && _dataAccess.GetById(client.Id) is not null)
        {
            throw new InvalidOperationException($"A client with ID {client.Id} already exists.");
        }

        DateTime now = DateTime.UtcNow;
        client.CreatedAt = now;
        client.UpdatedAt = now;
        _dataAccess.Add(client);
    }

    public void Update(int id, Client client)
    {
        if (id <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(id));
        }

        Validate(client);

        Client? existingClient = _dataAccess.GetById(id);
        if (existingClient is null)
        {
            return;
        }

        client.CreatedAt = existingClient.CreatedAt;
        client.UpdatedAt = DateTime.UtcNow;
        _dataAccess.Update(id, client);
    }

    public void Remove(int id)
    {
        if (id <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(id));
        }

        _dataAccess.Remove(id);
    }

    private static void Validate(Client client)
    {
        ArgumentNullException.ThrowIfNull(client);

        (string, string)[] requiredFields = new[]
        {
            (client.Name, nameof(client.Name)),
            (client.Address, nameof(client.Address)),
            (client.City, nameof(client.City)),
            (client.ZipCode, nameof(client.ZipCode)),
            (client.Province, nameof(client.Province)),
            (client.Country, nameof(client.Country)),
            (client.ContactName, nameof(client.ContactName)),
            (client.ContactPhone, nameof(client.ContactPhone)),
            (client.ContactEmail, nameof(client.ContactEmail)),
        };

        foreach ((string? value, string? name) in requiredFields)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException($"{name} is required.", name);
            }
        }

        if (!new EmailAddressAttribute().IsValid(client.ContactEmail))
        {
            throw new ArgumentException("ContactEmail must be a valid email address.", nameof(client.ContactEmail));
        }
    }
}
