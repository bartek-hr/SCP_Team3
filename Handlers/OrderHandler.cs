using CargoHUB.Framework;
using CargoHUB.Logics;
using CargoHUB.Models;

namespace CargoHUB.Handlers;

public sealed class OrderHandler(OrderLogic logic)
{
    [Get("/orders")]
    [Describe("Geeft alle orders terug.", Tags = ["Orders"], OperationId = "getOrders")]
    public Response<IReadOnlyList<Order>> GetAll() => Response.Ok(logic.GetAll());

    [Get("/orders/{id}")]
    [Describe("Geeft één record terug.", Tags = ["Orders"], OperationId = "getOrder")]
    public Response<Order> GetById(Request request)
    {
        if (!request.TryParam<int>("id", out int id) || id <= 0)
            return Response<Order>.BadRequest();

        Order? record = logic.GetById(id);
        return record is null ? Response<Order>.NotFound() : Response.Ok(record);
    }

    [Get("/orders/{id}/items")]
    [Describe("Geeft de artikelen terug.", Tags = ["Orders"], OperationId = "getOrderItems")]
    public Response<IReadOnlyList<OrderItem>> GetItems(Request request)
    {
        if (!request.TryParam<int>("id", out int id) || id <= 0)
            return Response<IReadOnlyList<OrderItem>>.BadRequest();

        return Response.Ok(logic.GetItems(id));
    }

    [Post("/orders")]
    [Describe("Maakt een record aan.", Tags = ["Orders"], OperationId = "createOrder", SuccessStatus = StatusCodes.Status201Created)]
    public Response Add(Request<Order> request)
    {
        try
        {
            logic.Add(request.Data);
            return Response.Created();
        }
        catch (ArgumentException)
        {
            return Response.BadRequest();
        }
        catch (InvalidOperationException)
        {
            return Response.Conflict();
        }
    }

    [Put("/orders/{id}")]
    [Describe("Wijzigt een record.", Tags = ["Orders"], OperationId = "updateOrder")]
    public Response Update(Request<Order> request)
    {
        if (!request.TryParam<int>("id", out int id) || id <= 0)
            return Response.BadRequest();
        if (logic.GetById(id) is null)
            return Response.NotFound();

        try
        {
            logic.Update(id, request.Data);
            return Response.Ok();
        }
        catch (ArgumentException)
        {
            return Response.BadRequest();
        }
    }

    [Put("/orders/{id}/items")]
    [Describe("Vervangt de artikelen en werkt de voorraad bij.", Tags = ["Orders"], OperationId = "replaceOrderItems")]
    public Response ReplaceItems(Request<List<OrderItem>> request)
    {
        if (!request.TryParam<int>("id", out int id) || id <= 0)
            return Response.BadRequest();
        if (logic.GetById(id) is null)
            return Response.NotFound();

        try
        {
            logic.ReplaceItems(id, request.Data);
            return Response.Ok();
        }
        catch (ArgumentException)
        {
            return Response.BadRequest();
        }
    }

    [Delete("/orders/{id}")]
    [Describe("Verwijdert een record.", Tags = ["Orders"], OperationId = "deleteOrder")]
    public Response Remove(Request request)
    {
        if (!request.TryParam<int>("id", out int id) || id <= 0)
            return Response.BadRequest();
        if (logic.GetById(id) is null)
            return Response.NotFound();

        logic.Remove(id);
        return Response.Ok();
    }
}
