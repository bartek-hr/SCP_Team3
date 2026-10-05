using CargoHUB.Framework;
using CargoHUB.Logics;
using CargoHUB.Models;

namespace CargoHUB.Handlers;

public sealed class SupplierHandler(SupplierLogic logic)
{
    [Get("/suppliers")]
    [Describe("Lists suppliers.", Tags = ["Suppliers"])]
    public Response<IReadOnlyList<Supplier>> GetAll() => Response.Ok(logic.GetAll());

    [Get("/suppliers/{id}")]
    [Describe("Gets a supplier.", Tags = ["Suppliers"])]
    public Response<Supplier> GetById(Request request)
    {
        if (!request.TryParam<int>("id", out int id) || id <= 0)
        {
            return Response<Supplier>.BadRequest();
        }

        Supplier? supplier = logic.GetById(id);
        return supplier is null ? Response<Supplier>.NotFound() : Response.Ok(supplier);
    }

    [Get("/suppliers/{id}/items")]
    [Describe("Gets a supplier's items.", Tags = ["Suppliers"])]
    public Response<IReadOnlyList<Item>> GetItems(Request request, ItemLogic itemLogic)
    {
        if (!request.TryParam<int>("id", out int id) || id <= 0)
        {
            return Response<IReadOnlyList<Item>>.BadRequest();
        }

        return logic.GetById(id) is null
            ? Response<IReadOnlyList<Item>>.NotFound()
            : Response.Ok(itemLogic.GetForSupplier(id));
    }

    [Post("/suppliers")]
    [Describe("Creates a supplier.", Tags = ["Suppliers"], SuccessStatus = 201)]
    public Response Add(Request<Supplier> request)
    {
        try
        {
            logic.Add(request.Data);
            return Response.Status(201);
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

    [Put("/suppliers/{id}")]
    [Describe("Updates a supplier.", Tags = ["Suppliers"])]
    public Response Update(Request<Supplier> request)
    {
        if (!request.TryParam<int>("id", out int id) || id <= 0)
        {
            return Response.BadRequest();
        }

        try
        {
            return logic.Update(id, request.Data) ? Response.Ok() : Response.NotFound();
        }
        catch (ArgumentException)
        {
            return Response.BadRequest();
        }
    }

    [Delete("/suppliers/{id}")]
    [Describe("Deletes a supplier.", Tags = ["Suppliers"])]
    public Response Remove(Request request)
    {
        if (!request.TryParam<int>("id", out int id) || id <= 0)
        {
            return Response.BadRequest();
        }

        return logic.Remove(id) ? Response.Ok() : Response.NotFound();
    }
}
