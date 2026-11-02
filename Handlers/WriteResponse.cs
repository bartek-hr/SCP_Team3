using CargoHUB.Framework;
using Microsoft.EntityFrameworkCore;

namespace CargoHUB.Handlers;

// The logic layer reports invalid input with ArgumentException (400) and a conflict with
// stored data with InvalidOperationException (409); the message becomes the problem detail.
internal static class WriteResponse
{
    public static Response Save(Action save, int statusCode)
    {
        return Save(() =>
        {
            save();
            return true;
        }, statusCode);
    }

    // For logic methods that return false when the record does not exist.
    public static Response Save(Func<bool> save, int statusCode)
    {
        try
        {
            return save() ? Response.Status(statusCode) : Response.NotFound();
        }
        catch (ArgumentException exception)
        {
            return Response.BadRequest(exception.Message);
        }
        catch (InvalidOperationException exception)
        {
            return Response.Conflict(exception.Message);
        }
        catch (DbUpdateException)
        {
            // Safety net for a unique index or foreign key that a concurrent request got to first.
            return Response.Conflict("The change conflicts with data that is already stored.");
        }
    }
}
