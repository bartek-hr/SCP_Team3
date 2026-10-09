using System.Text.Json.Serialization;

namespace CargoHUB.Models;

public sealed class ShipmentItem
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("shipment_id")]
    public int ShipmentId { get; set; }

    [JsonPropertyName("item_id")]
    public int ItemId { get; set; }

    [JsonPropertyName("amount")]
    public int Amount { get; set; }
}