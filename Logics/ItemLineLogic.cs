using CargoHUB.Access;
using CargoHUB.Models;

namespace CargoHUB.Logics;

public sealed class ItemLineLogic(ItemLineAccess itemLineAccess, ItemAccess itemAccess)
{

    // Python: get_item_lines()
    public IReadOnlyList<ItemLine> GetAll() => itemLineAccess.GetAll();

    // Python: get_item_line(item_line_id)
    public ItemLine? GetById(int id) => id > 0 ? itemLineAccess.GetById(id) : null;

    // Python: add_item_line(item_line)
    public void Add(ItemLine itemLine)
    {
        Validate(itemLine);
        if (itemLine.Id < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(itemLine.Id));
        }

        if (itemLine.Id > 0 && GetById(itemLine.Id) is not null)
        {
            throw new InvalidOperationException($"An item line with ID {itemLine.Id} already exists.");
        }

        itemLine.CreatedAt = DateTime.UtcNow;
        itemLine.UpdatedAt = itemLine.CreatedAt;
        itemLineAccess.Add(itemLine);
    }

    // Python: update_item_line(item_line_id, item_line)
    public bool Update(int id, ItemLine itemLine)
    {
        if (id <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(id));
        }

        if (GetById(id) is null)
        {
            return false;
        }

        Validate(itemLine);
        itemLine.UpdatedAt = DateTime.UtcNow;
        return itemLineAccess.Update(id, itemLine);
    }

    // Python: remove_item_line(item_line_id)
    public bool Remove(int id)
    {
        if (id <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(id));
        }

        if (GetById(id) is null)
        {
            return false;
        }

        // New in C#: Python removed the line even when items still pointed to it.
        if (itemAccess.AnyInItemLine(id))
        {
            throw new InvalidOperationException($"Item line {id} still has items.");
        }

        return itemLineAccess.Remove(id);
    }

    // New in C#: the Python version did no validation.
    private static void Validate(ItemLine itemLine)
    {
        ArgumentNullException.ThrowIfNull(itemLine);

        if (string.IsNullOrWhiteSpace(itemLine.Name))
        {
            throw new ArgumentException("Name is required.", nameof(itemLine));
        }
    }
}
