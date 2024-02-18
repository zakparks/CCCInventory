using Microsoft.EntityFrameworkCore;

namespace CCCInventory.Data
{
    // dotnet ef migrations add <migrationName>
    // dotnet ef database update
    public class DataContext : DbContext
    {
        public DataContext(DbContextOptions<DataContext> options) : base(options) { }

        public DbSet<Order> Orders => Set<Order>();
    }
}
