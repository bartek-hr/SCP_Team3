using System.Text.Json;
using CargoHUB.Datasource;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace CargoHUB.Maintenance;

public static class RepairCommand
{
    public static int Execute(string[] args)
    {
        if (args.Length is < 2 or > 3 || args[0] != "--repair-records"
            || (args.Length == 3 && args[2] != "--apply"))
        {
            Console.Error.WriteLine("Usage: --repair-records <existing-sqlite-path> [--apply]");
            return 2;
        }
        bool apply = args.Length == 3;
        try
        {
            string path = Path.GetFullPath(args[1]);
            if (!File.Exists(path)) throw new FileNotFoundException("Database must already exist.", path);
            var connection = new SqliteConnectionStringBuilder
            {
                DataSource = path,
                Mode = apply ? SqliteOpenMode.ReadWrite : SqliteOpenMode.ReadOnly,
                Pooling = false
            };
            using var db = new CargoHubDbContext(new DbContextOptionsBuilder<CargoHubDbContext>()
                .UseSqlite(connection.ToString()).Options);
            var report = new RecordRepair(db).Run(apply);
            Console.WriteLine(JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true }));
            return report.Records.Any(r => r.Outcome == "NeedsReview") ? 3 : 0;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine($"Record repair failed: {exception.Message}");
            return 1;
        }
    }
}
