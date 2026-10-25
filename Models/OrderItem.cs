using System.Text.Json.Serialization;

namespace CargoHUB.Models;

public sealed class OrderItem
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("order_id")]
    public int OrderId { get; set; }

    [JsonPropertyName("item_id")]
    public int ItemId { get; set; }

    [JsonPropertyName("amount")]
    public int Amount { get; set; }

    [JsonPropertyName("unit_price")]
    public decimal? UnitPrice { get; set; }
}