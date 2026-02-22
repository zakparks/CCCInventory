using Microsoft.EntityFrameworkCore;

namespace CCCInventory.Data
{
    // dotnet ef migrations add <migrationName>
    // dotnet ef database update
    public class DataContext : DbContext
    {
        public DataContext(DbContextOptions<DataContext> options) : base(options) { }

        public DbSet<Order> Orders => Set<Order>();
        public DbSet<Cake> Cakes => Set<Cake>();
        public DbSet<Cupcake> Cupcakes => Set<Cupcake>();
        public DbSet<Cookie> Cookies => Set<Cookie>();
        public DbSet<Pupcake> Pupcakes => Set<Pupcake>();
        public DbSet<OrderAttachment> OrderAttachments => Set<OrderAttachment>();
        public DbSet<OptionItem> OptionItems => Set<OptionItem>();
        public DbSet<SignatureCupcake> SignatureCupcakes => Set<SignatureCupcake>();
    }
}
