using Microsoft.EntityFrameworkCore;

namespace CCCInventory.Data
{
    public class DataContext : DbContext
    {
        public DataContext(DbContextOptions<DataContext> options) : base(options) { }

        public DbSet<Order> Orders => Set<Order>();
    }
}
