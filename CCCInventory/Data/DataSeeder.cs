using CCCInventory.Data;
using Microsoft.EntityFrameworkCore;

namespace CCCInventory
{
    public static class DataSeeder
    {
        public static async Task SeedAsync(IServiceProvider services)
        {
            using var scope = services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<DataContext>();
            var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();

            context.Database.Migrate();
            SeedData.Initialize(context);
            await SeedAuthAsync(context, config);
        }

        private static async Task SeedAuthAsync(DataContext context, IConfiguration config)
        {
            var initialPassword = config["Security:InitialAdminPassword"]
                ?? throw new InvalidOperationException(
                    "Security:InitialAdminPassword is not configured. " +
                    "Set it via the Security__InitialAdminPassword environment variable.");

            // Seed the initial admin user if none exists
            if (!await context.AppUsers.AnyAsync())
            {
                context.AppUsers.Add(new AppUser
                {
                    Username = "admin",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(initialPassword)
                });
                await context.SaveChangesAsync();
            }

            // Emergency recovery: if ForceResetAdminPassword is true, reset the admin password
            // to InitialAdminPassword regardless of what it currently is.
            // Set Security:ForceResetAdminPassword = true in appsettings, restart, log in,
            // then remove the flag and change your password normally.
            if (config.GetValue<bool>("Security:ForceResetAdminPassword"))
            {
                var admin = await context.AppUsers.FirstOrDefaultAsync(u => u.Username == "admin");
                if (admin != null)
                {
                    admin.PasswordHash = BCrypt.Net.BCrypt.HashPassword(initialPassword);
                    await context.SaveChangesAsync();
                    Console.WriteLine($"[DataSeeder] Admin password has been reset to the configured InitialAdminPassword.");
                }
            }

            // Seed a default staff member if none exist
            if (!await context.StaffMembers.AnyAsync())
            {
                context.StaffMembers.Add(new StaffMember
                {
                    Name = "Default Staff",
                    Pin = "1234",
                    IsActive = true
                });
                await context.SaveChangesAsync();
            }
        }
    }
}
