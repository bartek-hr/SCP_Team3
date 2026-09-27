using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;

namespace CargoHUB.Models;

[PrimaryKey(nameof(ItemId), nameof(LocationId))]
public sealed class Inventory
{
    [JsonPropertyName("item_id")]
    public int ItemId { get; set; }

    [JsonPropertyName("location_id")]
    public int LocationId { get; set; }

    [JsonPropertyName("quantity_on_hand")]
    public int QuantityOnHand { get; set; }

    [JsonPropertyName("quantity_expected")]
    public int QuantityExpected { get; set; }

    [JsonPropertyName("quantity_ordered")]
    public int QuantityOrdered { get; set; }

    [JsonPropertyName("quantity_allocated")]
    public int QuantityAllocated { get; set; }

    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("updated_at")]
    public DateTime UpdatedAt { get; set; }
}

public sealed class InventoryTotals
{
    [JsonPropertyName("total_expected")]
    public long TotalExpected { get; set; }

    [JsonPropertyName("total_ordered")]
    public long TotalOrdered { get; set; }

    [JsonPropertyName("total_allocated")]
    public long TotalAllocated { get; set; }

    [JsonPropertyName("total_available")]
    public long TotalAvailable { get; set; }
}
