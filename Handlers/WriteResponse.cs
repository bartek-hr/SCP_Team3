using CargoHUB.Framework;

namespace CargoHUB.Handlers;

internal static class WriteResponse
{
    public static Response Save(Action save, int statusCode)
    {
        try
        {
            save();
            return Response.Status(statusCode);
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
}
