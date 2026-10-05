using System.Text.Json.Serialization;

namespace CargoHUB.Models;

[JsonConverter(typeof(JsonStringEnumConverter<TransferStatus>))]
public enum TransferStatus
{
    Scheduled,
    Processed,
}
