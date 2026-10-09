using CargoHUB.Framework;
using CargoHUB.Logics;
using CargoHUB.Models;

namespace CargoHUB.Handlers;

public sealed class OrderItemHandler(OrderItemLogic logic)
{
    [Get("/orderitems")]
    [Describe("Geeft alle OrderItems terug.", Tags = ["OrderItems"], OperationId = "getItems")]
    public Response<IReadOnlyList<OrderItem>> GetAll() => Response.Ok(logic.GetAll());

    [Get("/orderitems/{id}")]
    [Describe("Geeft één record terug.", Tags = ["orderitems"], OperationId = "getOrderItemById")]
    public Response<OrderItem> GetById(Request request)
    {
        if (!request.TryParam<int>("id", out int id) || id <= 0)
        {
            return Response<OrderItem>.BadRequest();
        }

        OrderItem? record = logic.GetById(id);
        if (record is null)
        {
            return Response<OrderItem>.NotFound();
        }

        return Response.Ok(record);
    }

    [Get("/orderitems/orders/{id}")]
    [Describe("Geeft records terug op OrderId.", Tags = ["Orders"], OperationId = "getOrderItemsByOrderid")]
    public Response<IReadOnlyList<OrderItem>> GetItems(Request request)
    {
        if (!request.TryParam<int>("id", out int id) || id <= 0)
        {
            return Response<IReadOnlyList<OrderItem>>.BadRequest();
        }

        return Response.Ok(logic.GetItems(id));
    }
    [Post("/orderitems")]
    [Describe("Maakt een record aan.", Tags = ["orderitems"], OperationId = "createOrderItem", SuccessStatus = StatusCodes.Status201Created)]
    public Response Add(Request<OrderItem> request)
    {
        return WriteResponse.Save(() => logic.Add(request.Data), StatusCodes.Status201Created);
    }

    [Put("/orderitems/{id}")]
    [Describe("Wijzigt een record.", Tags = ["orderitems"], OperationId = "updateOrderItem")]
    public Response Update(Request<OrderItem> request)
    {
        if (!request.TryParam<int>("id", out int id) || id <= 0)
        {
            return Response.BadRequest();
        }
        if (logic.GetById(id) is null)
        {
            return Response.NotFound();
        }

        return WriteResponse.Save(() => logic.Update(id, request.Data), StatusCodes.Status200OK);
    }


    [Delete("/orderitems/{id}")]
    [Describe("Verwijdert een record.", Tags = ["orderitems"], OperationId = "deleteOrderItem")]
    public Response Remove(Request request)
    {
        if (!request.TryParam<int>("id", out int id) || id <= 0)
        {
            return Response.BadRequest();
        }
        if (logic.GetById(id) is null)
        {
            return Response.NotFound();
        }

        logic.Remove(id);
        return Response.Ok();
    }
}
