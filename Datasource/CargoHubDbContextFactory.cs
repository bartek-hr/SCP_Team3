using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace CargoHUB.Datasource;

public sealed class CargoHubDbContextFactory : IDesignTimeDbContextFactory<CargoHubDbContext>
{
    public CargoHubDbContext CreateDbContext(string[] args)
    {
        var dataDirectory = Path.Combine(Directory.GetCurrentDirectory(), "Datasource");
        Directory.CreateDirectory(dataDirectory);

        var databasePath = Path.Combine(dataDirectory, "data.sqlite");
        var options = new DbContextOptionsBuilder<CargoHubDbContext>()
            .UseSqlite($"Data Source={databasePath}")
            .Options;

        return new CargoHubDbContext(options);
    }
}
