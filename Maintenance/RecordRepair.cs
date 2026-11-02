using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Text.Json;
using CargoHUB.Datasource;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace CargoHUB.Maintenance;

public sealed record RepairFinding(string Field, string Rule, bool Recoverable);
public sealed record RepairResult(string Table, IReadOnlyDictionary<string, object?> Key,
    string Outcome, IReadOnlyList<RepairFinding> Reasons,
    IReadOnlyDictionary<string, object?> Before, IReadOnlyDictionary<string, object?> After);
public sealed record RepairReport(string RunId, string RulesVersion, bool Applied, int Scanned,
    IReadOnlyList<RepairResult> Records);

/// <summary>Offline maintenance only. Never infer identities, stock, dates or relationships.</summary>
public sealed class RecordRepair(CargoHubDbContext db)
{
    public const string RulesVersion = "74-v1";
    private static readonly HashSet<string> TrimFields =
        ["Name", "Description", "Address", "City", "Province", "Country", "ContactName"];
    private static readonly HashSet<string> RequiredText =
        ["Reference", "CustomerPoNumber", "OrderStatus", "ShipmentType", "ShipmentStatus",
         "CarrierName", "ShippingMethod", "PaymentType"];
    private static readonly HashSet<string> Nonnegative =
        ["QuantityOnHand", "QuantityExpected", "QuantityOrdered", "QuantityAllocated",
         "UnitWeight", "MinPurchaseQty", "CaseSize", "OrderMultiple", "UnitPrice"];
    // Includes logical relationships not enforced by the legacy database schema.
    private static readonly Dictionary<string, string> References = new()
    {
        ["ClientId"] = "Clients", ["ShipToClientId"] = "Clients", ["BillToClientId"] = "Clients",
        ["WarehouseId"] = "Warehouses", ["LocationId"] = "Locations",
        ["FromLocationId"] = "Locations", ["ToLocationId"] = "Locations",
        ["ItemId"] = "Items", ["ItemLineId"] = "ItemLines", ["ItemGroupId"] = "ItemGroups",
        ["ItemTypeId"] = "ItemTypes", ["SupplierId"] = "Suppliers",
        ["OrderId"] = "Orders", ["ShipmentId"] = "Shipments", ["TransferId"] = "Transfers"
    };

    public RepairReport Run(bool apply = false)
    {
        var connection = (SqliteConnection)db.Database.GetDbConnection();
        bool wasClosed = connection.State != System.Data.ConnectionState.Open;
        if (wasClosed) connection.Open();
        try
        {
            // An immediate transaction prevents another writer changing our snapshots before repair.
            using var transaction = connection.BeginTransaction(deferred: !apply);
            var entities = db.Model.GetEntityTypes().OrderBy(e => e.GetTableName()).ToArray();
            var tables = entities.ToDictionary(e => e.GetTableName()!, e => Read(e, connection, transaction));
            var identities = entities.ToDictionary(e => e.GetTableName()!, e =>
            {
                var key = e.FindPrimaryKey()!.Properties;
                return key.Count == 1 ? tables[e.GetTableName()!]
                    .Select(r => Convert.ToString(r[key[0].GetColumnName()], CultureInfo.InvariantCulture)!)
                    .ToHashSet() : [];
            });
            string runId = Guid.NewGuid().ToString();
            var results = new List<RepairResult>();
            int scanned = 0;
            foreach (var entity in entities)
            {
                string table = entity.GetTableName()!;
                foreach (var before in tables[table])
                {
                    scanned++;
                    var after = new Dictionary<string, object?>(before);
                    var reasons = new List<RepairFinding>();
                    foreach (var property in entity.GetProperties())
                    {
                        string column = property.GetColumnName();
                        object? value = before[column];
                        string name = property.Name;
                        Type type = Nullable.GetUnderlyingType(property.ClrType) ?? property.ClrType;
                        void Flag(string rule) => reasons.Add(new(column, rule, false));
                        if (value is null)
                        {
                            if (!property.IsNullable) Flag("required-value-missing");
                            continue;
                        }
                        if (type == typeof(string))
                        {
                            if (value is not string text) { Flag("invalid-text"); continue; }
                            bool required = property.PropertyInfo?.IsDefined(typeof(RequiredAttribute), true) == true
                                || RequiredText.Contains(name);
                            if (required && string.IsNullOrWhiteSpace(text)) Flag("required-text-empty");
                            else if (TrimFields.Contains(name) && text != text.Trim())
                            {
                                after[column] = text.Trim();
                                reasons.Add(new(column, "descriptive-text-surrounding-whitespace", true));
                            }
                            if (name == "ContactEmail" && !new EmailAddressAttribute().IsValid(text))
                                Flag("invalid-email");
                        }
                        else if (type == typeof(DateTime))
                        {
                            if (value is not string date || !DateTime.TryParse(date, CultureInfo.InvariantCulture,
                                DateTimeStyles.RoundtripKind, out var parsed) || parsed == default)
                                Flag("invalid-or-missing-date");
                        }
                        else if (type.IsEnum && property.GetTypeMapping().Converter?.ProviderClrType == typeof(string))
                        {
                            if (value is not string enumText || !Enum.GetNames(type).Contains(enumText))
                                Flag("unknown-enum-value");
                        }
                        else if (type == typeof(int) || type == typeof(decimal) || type.IsEnum)
                        {
                            if (!decimal.TryParse(Convert.ToString(value, CultureInfo.InvariantCulture),
                                    NumberStyles.Float, CultureInfo.InvariantCulture, out decimal number)
                                || (type != typeof(decimal) && (number != decimal.Truncate(number)
                                    || number < int.MinValue || number > int.MaxValue)))
                            { Flag("invalid-number"); continue; }
                            if (type.IsEnum && !Enum.IsDefined(type, (int)number)) Flag("unknown-enum-value");
                            if ((property.IsPrimaryKey() || References.ContainsKey(name) || name == "Amount") && number <= 0)
                                Flag("nonpositive-identity-or-amount");
                            if (Nonnegative.Contains(name) && number < 0) Flag("negative-quantity-or-value");
                            if (References.TryGetValue(name, out string? target)
                                && !identities[target].Contains(number.ToString(CultureInfo.InvariantCulture)))
                                Flag("missing-related-record:" + target);
                        }
                    }
                    var dates = entity.GetProperties().Where(p => p.ClrType == typeof(DateTime))
                        .ToDictionary(p => p.Name, p => before[p.GetColumnName()]);
                    if (dates.TryGetValue("CreatedAt", out var created) && dates.TryGetValue("UpdatedAt", out var updated)
                        && DateTime.TryParse(created as string, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var c)
                        && DateTime.TryParse(updated as string, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var u)
                        && u < c)
                        reasons.Add(new("updated_at", "updated-before-created", false));
                    if (reasons.Count == 0) continue;
                    var key = entity.FindPrimaryKey()!.Properties.ToDictionary(p => p.GetColumnName(), p => before[p.GetColumnName()]);
                    bool blocked = reasons.Any(r => !r.Recoverable);
                    if (blocked) after = new(before); // No partial repairs of an ambiguous record.
                    var result = new RepairResult(table, key,
                        blocked ? "NeedsReview" : apply ? "Repaired" : "Recoverable", reasons, before, after);
                    results.Add(result);
                    if (apply && !blocked) Update(result, connection, transaction);
                }
            }
            var report = new RepairReport(runId, RulesVersion, apply, scanned, results);
            if (apply) Audit(report, connection, transaction);
            transaction.Commit();
            return report;
        }
        finally { if (wasClosed) connection.Close(); }
    }

    private static List<Dictionary<string, object?>> Read(IEntityType entity, SqliteConnection connection, SqliteTransaction transaction)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = $"SELECT * FROM {Quote(entity.GetTableName()!)} ORDER BY " +
            string.Join(",", entity.FindPrimaryKey()!.Properties.Select(p => Quote(p.GetColumnName())));
        using var reader = command.ExecuteReader();
        var rows = new List<Dictionary<string, object?>>();
        while (reader.Read())
        {
            var row = new Dictionary<string, object?>();
            for (int i = 0; i < reader.FieldCount; i++) row[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);
            rows.Add(row);
        }
        return rows;
    }

    private static void Update(RepairResult result, SqliteConnection connection, SqliteTransaction transaction)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        var changed = result.After.Keys.Where(k => !Equals(result.Before[k], result.After[k])).ToArray();
        command.CommandText = $"UPDATE {Quote(result.Table)} SET " + string.Join(",", changed.Select((k, i) => $"{Quote(k)} = $v{i}")) +
            " WHERE " + string.Join(" AND ", result.Key.Keys.Select((k, i) => $"{Quote(k)} = $k{i}"));
        for (int i = 0; i < changed.Length; i++) command.Parameters.AddWithValue($"$v{i}", result.After[changed[i]]!);
        int index = 0;
        foreach (var value in result.Key.Values) command.Parameters.AddWithValue($"$k{index++}", value!);
        if (command.ExecuteNonQuery() != 1) throw new InvalidOperationException("Repair target changed; rolling back.");
    }

    private static void Audit(RepairReport report, SqliteConnection connection, SqliteTransaction transaction)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        // Deliberately independent of domain FKs: evidence must survive future archival (#85).
        command.CommandText = """
            CREATE TABLE IF NOT EXISTS RecordRepairRuns (
                RunId TEXT PRIMARY KEY, RulesVersion TEXT NOT NULL, RecordedAt TEXT NOT NULL,
                Scanned INTEGER NOT NULL, ReportJson TEXT NOT NULL);
            INSERT INTO RecordRepairRuns (RunId, RulesVersion, RecordedAt, Scanned, ReportJson)
            VALUES ($id, $version, $at, $scanned, $report);
            """;
        command.Parameters.AddWithValue("$id", report.RunId);
        command.Parameters.AddWithValue("$version", RulesVersion);
        command.Parameters.AddWithValue("$at", DateTime.UtcNow.ToString("O"));
        command.Parameters.AddWithValue("$scanned", report.Scanned);
        command.Parameters.AddWithValue("$report", JsonSerializer.Serialize(report));
        command.ExecuteNonQuery();
    }

    private static string Quote(string identifier) => "\"" + identifier.Replace("\"", "\"\"") + "\"";
}
