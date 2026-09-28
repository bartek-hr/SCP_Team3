using System.Text.Json.Serialization;

namespace CargoHUB.Models;

public sealed class TransferItem
{
    // Surrogate key and owner are storage details; the API only exposes item_id and amount.
    [JsonIgnore]
    public int Id { get; set; }

    [JsonIgnore]
    public int TransferId { get; set; }

    [JsonPropertyName("item_id")]
    public int ItemId { get; set; }

    [JsonPropertyName("amount")]
    public int Amount { get; set; }
}
