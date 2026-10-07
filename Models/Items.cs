using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace CargoHUB.Models;

public sealed class Item
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [Required]
    [JsonPropertyName("code")]
    public string Code { get; set; } = string.Empty;

    [Required]
    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    // Stored as text so leading zeros survive.
    [JsonPropertyName("barcode")]
    public string Barcode { get; set; } = string.Empty;

    [JsonPropertyName("model_number")]
    public string ModelNumber { get; set; } = string.Empty;

    [JsonPropertyName("commodity_code")]
    public int CommodityCode { get; set; }

    [JsonPropertyName("unit_weight")]
    public decimal UnitWeight { get; set; }

    [JsonPropertyName("item_line_id")]
    public int ItemLineId { get; set; }

    [JsonPropertyName("item_group_id")]
    public int ItemGroupId { get; set; }

    [JsonPropertyName("item_type_id")]
    public int ItemTypeId { get; set; }

    [JsonPropertyName("min_purchase_qty")]
    public int MinPurchaseQty { get; set; }

    [JsonPropertyName("case_size")]
    public int CaseSize { get; set; }

    [JsonPropertyName("packaging_type")]
    public string PackagingType { get; set; } = string.Empty;

    [JsonPropertyName("order_multiple")]
    public int OrderMultiple { get; set; }

    [JsonPropertyName("supplier_id")]
    public int SupplierId { get; set; }

    [JsonPropertyName("supplier_sku")]
    public string SupplierSku { get; set; } = string.Empty;

    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("updated_at")]
    public DateTime UpdatedAt { get; set; }
}
