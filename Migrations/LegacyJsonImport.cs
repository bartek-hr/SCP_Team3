using System.Text.Json;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CargoHUB.Migrations;

internal static class LegacyJsonImport
{
    public static void Import(MigrationBuilder migrationBuilder, string table, string file)
    {
        using Stream stream = typeof(LegacyJsonImport).Assembly
            .GetManifestResourceStream($"CargoHUB.LegacyData.{file}Json");
        if (stream is null)
        {
            throw new InvalidOperationException($"Legacybestand {file}.json ontbreekt.");
        }

        using JsonDocument document = JsonDocument.Parse(stream);
        JsonElement rows = document.RootElement;
        string[] names = rows[0].EnumerateObject().Select(property => property.Name).ToArray();
        string[] columns = names.ToArray();
        if (table == "Inventories")
        {
            columns = names.Select(name => string.Concat(name.Split('_')
                .Select(part => char.ToUpperInvariant(part[0]) + part.Substring(1)))).ToArray();
        }

        var values = new object[rows.GetArrayLength(), names.Length];
        for (int row = 0; row < rows.GetArrayLength(); row++)
        {
            for (int column = 0; column < names.Length; column++)
            {
                JsonElement value = rows[row].GetProperty(names[column]);
                if (value.ValueKind == JsonValueKind.Number && names[column] == "unit_price")
                {
                    values[row, column] = value.GetDecimal();
                }
                else if (value.ValueKind == JsonValueKind.Number)
                {
                    values[row, column] = value.GetInt32();
                }
                else if (value.ValueKind == JsonValueKind.String)
                {
                    values[row, column] = value.GetString();
                    if (names[column].EndsWith("_at") || names[column].EndsWith("_date"))
                    {
                        values[row, column] = value.GetDateTime();
                    }
                }
            }
        }

        migrationBuilder.InsertData(table, columns, values);
    }
}
