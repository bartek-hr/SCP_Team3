using CargoHUB.Access;
using CargoHUB.Logics;
using CargoHUB.Models;
using CargoHUB.Tests.Infrastructure;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CargoHUB.Tests.Logics;

[TestClass]
public sealed class ClientLogicTests : DatabaseTest
{
    private ClientLogic Logic => new(new ClientDataAccess(Context));

    protected override void SeedDatabase()
    {
        Context.Clients.Add(CreateClient(1));
        Context.SaveChanges();
    }

    [TestMethod]
    public void GetAllReturnsStoredClients()
    {
        IReadOnlyList<Client> clients = Logic.GetAll();

        Assert.AreEqual(1, clients.Count);
        Assert.AreEqual(1, clients[0].Id);
    }

    [TestMethod]
    public void GetByIdReturnsStoredClient()
    {
        Client? client = Logic.GetById(1);

        Assert.IsNotNull(client);
        Assert.AreEqual("Test Client 1", client.Name);
    }

    [TestMethod]
    public void GetByIdReturnsNullForMissingAndInvalidIds()
    {
        Assert.IsNull(Logic.GetById(999));
        Assert.IsNull(Logic.GetById(0));
        Assert.IsNull(Logic.GetById(-1));
    }

    [TestMethod]
    public void AddPersistsClientAndSetsTimestamps()
    {
        Client client = CreateClient();

        Logic.Add(client);

        Client? storedClient = Logic.GetById(client.Id);
        Assert.IsNotNull(storedClient);
        Assert.IsTrue(storedClient.CreatedAt > DateTime.UnixEpoch);
        Assert.IsTrue(storedClient.UpdatedAt >= storedClient.CreatedAt);
    }

    [TestMethod]
    public void AddRejectsNullAndInvalidClients()
    {
        Assert.ThrowsException<ArgumentNullException>(() => Logic.Add(null!));

        Client invalidClient = CreateClient();
        invalidClient.ContactEmail = "invalid-email";

        Assert.ThrowsException<ArgumentException>(() => Logic.Add(invalidClient));
    }

    [TestMethod]
    public void UpdatePersistsChangesAndPreservesCreationTime()
    {
        DateTime createdAt = Logic.GetById(1)!.CreatedAt;
        Client updatedClient = CreateClient(name: "Updated Client");
        updatedClient.CreatedAt = DateTime.UtcNow.AddYears(1);

        Logic.Update(1, updatedClient);

        Client? storedClient = Logic.GetById(1);
        Assert.IsNotNull(storedClient);
        Assert.AreEqual("Updated Client", storedClient.Name);
        Assert.AreEqual(createdAt, storedClient.CreatedAt);
        Assert.IsTrue(storedClient.UpdatedAt > createdAt);
    }

    [TestMethod]
    public void UpdateDoesNothingForMissingClient()
    {
        Logic.Update(999, CreateClient(name: "Missing Client"));

        Assert.AreEqual(1, Logic.GetAll().Count);
    }

    [TestMethod]
    public void UpdateRejectsInvalidData()
    {
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => Logic.Update(0, CreateClient()));

        Client invalidClient = CreateClient();
        invalidClient.Name = string.Empty;

        Assert.ThrowsException<ArgumentException>(() => Logic.Update(1, invalidClient));
    }

    [TestMethod]
    public void RemoveDeletesStoredClient()
    {
        Logic.Remove(1);

        Assert.IsNull(Logic.GetById(1));
    }

    [TestMethod]
    public void RemoveDoesNothingForMissingClient()
    {
        Logic.Remove(999);

        Assert.AreEqual(1, Logic.GetAll().Count);
    }

    [TestMethod]
    public void RemoveRejectsInvalidIds()
    {
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => Logic.Remove(0));
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => Logic.Remove(-1));
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
