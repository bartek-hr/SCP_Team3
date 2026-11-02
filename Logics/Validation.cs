namespace CargoHUB.Logics;

// Shared input checks for the logic classes. The handlers return the message to the client,
// so it names the JSON field. ArgumentException means invalid input (400).
internal static class Validation
{
    public static void Required(string? value, string field)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException($"{field} is required.");
        }
    }

    public static void Required(DateTime value, string field)
    {
        if (value == default)
        {
            throw new ArgumentException($"{field} is required.");
        }
    }

    public static void Positive(int value, string field)
    {
        if (value <= 0)
        {
            throw new ArgumentException($"{field} must be greater than 0.");
        }
    }

    public static void NotNegative(int value, string field)
    {
        if (value < 0)
        {
            throw new ArgumentException($"{field} cannot be negative.");
        }
    }

    public static void NotNegative(decimal? value, string field)
    {
        if (value < 0)
        {
            throw new ArgumentException($"{field} cannot be negative.");
        }
    }

    // A reference must point to a stored record; 0 means the field was not sent.
    public static void Reference(int id, string field, Func<int, bool> exists)
    {
        if (id <= 0 || !exists(id))
        {
            throw new ArgumentException($"{field} {id} does not exist.");
        }
    }

    // The id in the body may be left out (0), but must otherwise match the id in the URL.
    public static void MatchesRoute(int bodyId, int routeId)
    {
        if (bodyId != 0 && bodyId != routeId)
        {
            throw new ArgumentException($"id {bodyId} in the body does not match id {routeId} in the URL.");
        }
    }

    // Shared by the item lists of orders, shipments and transfers.
    public static void Items<T>(IReadOnlyList<T>? items, Func<T, int> itemId, Func<T, int> amount, Func<int, bool> itemExists)
        where T : class
    {
        if (items is null)
        {
            throw new ArgumentException("items is required.");
        }

        var itemIds = new HashSet<int>();
        for (int index = 0; index < items.Count; index++)
        {
            T item = items[index] ?? throw new ArgumentException($"items[{index}] is required.");
            Reference(itemId(item), $"items[{index}].item_id", itemExists);
            Positive(amount(item), $"items[{index}].amount");
            if (!itemIds.Add(itemId(item)))
            {
                throw new ArgumentException($"item_id {itemId(item)} appears more than once in items.");
            }
        }
    }
}
