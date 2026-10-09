using System.Text.Json.Serialization;

namespace CargoHUB.Models;

public sealed class Order
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("client_id")]
    public int ClientId { get; set; }

    [JsonPropertyName("order_date")]
    public DateTime OrderDate { get; set; }

    [JsonPropertyName("request_date")]
    public DateTime RequestDate { get; set; }

    [JsonPropertyName("reference")]
    public string Reference { get; set; } = string.Empty;

    [JsonPropertyName("customer_po_number")]
    public string CustomerPoNumber { get; set; } = string.Empty;

    [JsonPropertyName("order_status")]
    public string OrderStatus { get; set; } = string.Empty;

    [JsonPropertyName("shipping_notes")]
    public string? ShippingNotes { get; set; }

    [JsonPropertyName("warehouse_id")]
    public int WarehouseId { get; set; }

    [JsonPropertyName("ship_to_client_id")]
    public int ShipToClientId { get; set; }

    [JsonPropertyName("bill_to_client_id")]
    public int BillToClientId { get; set; }

    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("updated_at")]
    public DateTime UpdatedAt { get; set; }

    [JsonPropertyName("items")]
    public List<OrderItem> Items { get; set; } = [];
}

