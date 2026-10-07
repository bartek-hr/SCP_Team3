using System.Text.Json.Serialization;

namespace CargoHUB.Models;

public sealed class OrderItem
{
    // Surrogate key and owner are storage details; the API only exposes the item fields.
    [JsonIgnore]
    public int Id { get; set; }

    [JsonIgnore]
    public int OrderId { get; set; }

    [JsonPropertyName("item_id")]
    public int ItemId { get; set; }

    [JsonPropertyName("amount")]
    public int Amount { get; set; }

    [JsonPropertyName("unit_price")]
    public decimal? UnitPrice { get; set; }
}
