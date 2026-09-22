using CargoHUB.Framework;
using CargoHUB.Logics;
using CargoHUB.Models;

namespace CargoHUB.Handlers;

public sealed class ClientHandler
{
    private readonly ClientLogic _logic;

    public ClientHandler(ClientLogic logic)
    {
        _logic = logic ?? throw new ArgumentNullException(nameof(logic));
    }

    [Get("/clients")]
    [Describe("Returns all clients.", Tags = ["Clients"], OperationId = "getClients")]
    public Response<IReadOnlyList<Client>> GetAll() => Response.Ok(_logic.GetAll());

    [Get("/clients/{id}")]
    [Describe("Returns a client.", Tags = ["Clients"], OperationId = "getClient")]
    public Response<Client> GetById(Request request)
    {
        if (!request.TryParam<int>("id", out int id))
        {
            return Response<Client>.BadRequest();
        }

        Client? client = _logic.GetById(id);
        if (client is null)
        {
            return Response<Client>.NotFound();
        }

        return Response.Ok(client);
    }

    [Get("/clients/{id}/orders")]
    [Describe("Returns orders for a client.", Tags = ["Clients"], OperationId = "getClientOrders")]
    public Response GetOrders()
    {
        return Response.NotImplemented();
    }

    [Post("/clients")]
    [Describe("Creates a client.", Tags = ["Clients"], OperationId = "createClient")]
    public Response Add(Request<Client> request)
    {
        try
        {
            _logic.Add(request.Data);
            return Response.Ok();
        }
        catch (ArgumentException)
        {
            return Response.BadRequest();
        }
    }

    [Put("/clients/{id}")]
    [Describe("Updates a client.", Tags = ["Clients"], OperationId = "updateClient")]
    public Response Update(Request<Client> request)
    {
        if (!request.TryParam<int>("id", out int id))
        {
            return Response.BadRequest();
        }

        if (_logic.GetById(id) is null)
        {
            return Response.NotFound();
        }

        try
        {
            _logic.Update(id, request.Data);
            return Response.Ok();
        }
        catch (ArgumentException)
        {
            return Response.BadRequest();
        }
    }

    [Delete("/clients/{id}")]
    [Describe("Removes a client.", Tags = ["Clients"], OperationId = "deleteClient")]
    public Response Remove(Request request)
    {
        if (!request.TryParam<int>("id", out int id))
        {
            return Response.BadRequest();
        }

        if (_logic.GetById(id) is null)
        {
            return Response.NotFound();
        }

        _logic.Remove(id);
        return Response.Ok();
    }
}
