using System.Text.Json.Serialization;

namespace CargoHUB.Models;

public sealed class Shipment
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("reference")]
    public string Reference { get; set; } = string.Empty;

    [JsonPropertyName("order_id")]
    public int? OrderId { get; set; }

    [JsonPropertyName("shipment_date")]
    public DateTime ShipmentDate { get; set; }

    [JsonPropertyName("shipment_type")]
    public string ShipmentType { get; set; } = string.Empty;

    [JsonPropertyName("shipment_status")]
    public string ShipmentStatus { get; set; } = string.Empty;

    [JsonPropertyName("carrier_name")]
    public string CarrierName { get; set; } = string.Empty;

    [JsonPropertyName("shipping_method")]
    public string ShippingMethod { get; set; } = string.Empty;

    [JsonPropertyName("payment_type")]
    public string PaymentType { get; set; } = string.Empty;

    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("updated_at")]
    public DateTime UpdatedAt { get; set; }

    [JsonPropertyName("items")]
    public List<ShipmentItem> Items { get; set; } = [];
}

public sealed class ShipmentItem
{
    [JsonIgnore]
    public int Id { get; set; }

    [JsonIgnore]
    public int ShipmentId { get; set; }

    [JsonPropertyName("item_id")]
    public int ItemId { get; set; }

    [JsonPropertyName("amount")]
    public int Amount { get; set; }
}
