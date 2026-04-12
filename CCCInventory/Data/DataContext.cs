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
        public DbSet<OtherItem> OtherItems => Set<OtherItem>();

        // Auth
        public DbSet<AppUser> AppUsers => Set<AppUser>();
        public DbSet<StaffMember> StaffMembers => Set<StaffMember>();
        public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

        // Customers
        public DbSet<Customer> Customers => Set<Customer>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<AppUser>()
                .HasIndex(u => u.Username)
                .IsUnique();

            modelBuilder.Entity<AuditLog>()
                .HasOne(a => a.StaffMember)
                .WithMany()
                .HasForeignKey(a => a.StaffMemberId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<AuditLog>()
                .HasIndex(a => new { a.EntityType, a.EntityId });

            modelBuilder.Entity<Customer>()
                .HasMany(c => c.Orders)
                .WithOne()
                .HasForeignKey(o => o.CustomerId)
                .OnDelete(DeleteBehavior.SetNull);
        }
    }
}
