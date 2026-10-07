using System.Text.Json.Serialization;

namespace CargoHUB.Models;

public sealed class ShipmentItem
{
    // Surrogate key and owner are storage details; the API only exposes the item fields.
    [JsonIgnore]
    public int Id { get; set; }

    [JsonIgnore]
    public int ShipmentId { get; set; }

    [JsonPropertyName("item_id")]
    public int ItemId { get; set; }

    [JsonPropertyName("amount")]
    public int Amount { get; set; }
}
