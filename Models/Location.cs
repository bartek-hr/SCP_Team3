using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace CargoHUB.Models;

public sealed class Location
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("warehouse_id")]
    public int WarehouseId { get; set; }

    [Required]
    [JsonPropertyName("code")]
    public string Code { get; set; } = string.Empty;

    [Required]
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("updated_at")]
    public DateTime UpdatedAt { get; set; }
}
