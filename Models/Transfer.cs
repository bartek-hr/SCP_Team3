using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace CargoHUB.Models;

public sealed class Transfer
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [Required]
    [JsonPropertyName("reference")]
    public string Reference { get; set; } = string.Empty;

    [JsonPropertyName("from_location_id")]
    public int FromLocationId { get; set; }

    [JsonPropertyName("to_location_id")]
    public int ToLocationId { get; set; }

    [JsonPropertyName("transfer_status")]
    public TransferStatus TransferStatus { get; set; }

    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("updated_at")]
    public DateTime UpdatedAt { get; set; }

    // Null means "not sent": an update then keeps the stored items, like the legacy API.
    [JsonPropertyName("items")]
    public List<TransferItem>? Items { get; set; }
}
