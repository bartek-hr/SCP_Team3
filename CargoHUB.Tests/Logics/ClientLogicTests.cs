using CargoHUB.Access;
using CargoHUB.Datasource;
using CargoHUB.Logics;
using CargoHUB.Models;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CargoHUB.Tests.Logics;

[TestClass]
public sealed class ClientLogicTests
{
    private SqliteConnection _connection = null!;
    private CargoHubDbContext _context = null!;
    private ClientLogic _logic = null!;

    [TestInitialize]
    public void Initialize()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        DbContextOptions<CargoHubDbContext> options = new DbContextOptionsBuilder<CargoHubDbContext>()
            .UseSqlite(_connection)
            .Options;
        _context = new CargoHubDbContext(options);
        _context.Database.EnsureCreated();
        _context.Clients.Add(CreateClient(1));
        _context.SaveChanges();
        _logic = new ClientLogic(new ClientDataAccess(_context));
    }

    [TestCleanup]
    public void Cleanup()
    {
        _context.Dispose();
        _connection.Dispose();
    }

    [TestMethod]
    public void GetAllReturnsStoredClients()
    {
        IReadOnlyList<Client> clients = _logic.GetAll();

        Assert.AreEqual(1, clients.Count);
        Assert.AreEqual(1, clients[0].Id);
    }

    [TestMethod]
    public void GetByIdReturnsStoredClient()
    {
        Client? client = _logic.GetById(1);

        Assert.IsNotNull(client);
        Assert.AreEqual("Test Client 1", client.Name);
    }

    [TestMethod]
    public void GetByIdReturnsNullForMissingAndInvalidIds()
    {
        Assert.IsNull(_logic.GetById(999));
        Assert.IsNull(_logic.GetById(0));
        Assert.IsNull(_logic.GetById(-1));
    }

    [TestMethod]
    public void AddPersistsClientAndSetsTimestamps()
    {
        Client client = CreateClient();

        _logic.Add(client);

        Client? storedClient = _logic.GetById(client.Id);
        Assert.IsNotNull(storedClient);
        Assert.IsTrue(storedClient.CreatedAt > DateTime.UnixEpoch);
        Assert.IsTrue(storedClient.UpdatedAt >= storedClient.CreatedAt);
    }

    [TestMethod]
    public void AddRejectsNullAndInvalidClients()
    {
        Assert.ThrowsException<ArgumentNullException>(() => _logic.Add(null!));

        Client invalidClient = CreateClient();
        invalidClient.ContactEmail = "invalid-email";

        Assert.ThrowsException<ArgumentException>(() => _logic.Add(invalidClient));
    }

    [TestMethod]
    public void UpdatePersistsChangesAndPreservesCreationTime()
    {
        DateTime createdAt = _logic.GetById(1)!.CreatedAt;
        Client updatedClient = CreateClient(name: "Updated Client");
        updatedClient.CreatedAt = DateTime.UtcNow.AddYears(1);

        _logic.Update(1, updatedClient);

        Client? storedClient = _logic.GetById(1);
        Assert.IsNotNull(storedClient);
        Assert.AreEqual("Updated Client", storedClient.Name);
        Assert.AreEqual(createdAt, storedClient.CreatedAt);
        Assert.IsTrue(storedClient.UpdatedAt > createdAt);
    }

    [TestMethod]
    public void UpdateDoesNothingForMissingClient()
    {
        _logic.Update(999, CreateClient(name: "Missing Client"));

        Assert.AreEqual(1, _logic.GetAll().Count);
    }

    [TestMethod]
    public void UpdateRejectsInvalidData()
    {
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => _logic.Update(0, CreateClient()));

        Client invalidClient = CreateClient();
        invalidClient.Name = string.Empty;

        Assert.ThrowsException<ArgumentException>(() => _logic.Update(1, invalidClient));
    }

    [TestMethod]
    public void RemoveDeletesStoredClient()
    {
        _logic.Remove(1);

        Assert.IsNull(_logic.GetById(1));
    }

    [TestMethod]
    public void RemoveDoesNothingForMissingClient()
    {
        _logic.Remove(999);

        Assert.AreEqual(1, _logic.GetAll().Count);
    }

    [TestMethod]
    public void RemoveRejectsInvalidIds()
    {
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => _logic.Remove(0));
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => _logic.Remove(-1));
    }

    private static Client CreateClient(int id = 0, string name = "Test Client") =>
        new()
        {
            Id = id,
            Name = id == 0 ? name : $"{name} {id}",
            Address = "Test Street 1",
            City = "Amersfoort",
            ZipCode = "3811AA",
            Province = "Utrecht",
            Country = "Netherlands",
            ContactName = "Test Contact",
            ContactPhone = "+31 600000000",
            ContactEmail = "test@example.com",
            CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            UpdatedAt = new DateTime(2024, 1, 2, 0, 0, 0, DateTimeKind.Utc),
        };
}
