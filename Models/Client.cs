using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace CargoHUB.Models;

public sealed class Client
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [Required]
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [Required]
    [JsonPropertyName("address")]
    public string Address { get; set; } = string.Empty;

    [Required]
    [JsonPropertyName("city")]
    public string City { get; set; } = string.Empty;

    [Required]
    [JsonPropertyName("zip_code")]
    public string ZipCode { get; set; } = string.Empty;

    [Required]
    [JsonPropertyName("province")]
    public string Province { get; set; } = string.Empty;

    [Required]
    [JsonPropertyName("country")]
    public string Country { get; set; } = string.Empty;

    [Required]
    [JsonPropertyName("contact_name")]
    public string ContactName { get; set; } = string.Empty;

    [Required]
    [JsonPropertyName("contact_phone")]
    public string ContactPhone { get; set; } = string.Empty;

    [Required]
    [JsonPropertyName("contact_email")]
    public string ContactEmail { get; set; } = string.Empty;

    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("updated_at")]
    public DateTime UpdatedAt { get; set; }
}
