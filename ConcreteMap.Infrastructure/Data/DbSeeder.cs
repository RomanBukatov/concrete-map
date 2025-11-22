using ConcreteMap.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using BCrypt.Net;

namespace ConcreteMap.Infrastructure.Data
{
    public static class DbSeeder
    {
        public static async Task SeedUsers(ApplicationDbContext context)
        {
            if (await context.Users.AnyAsync())
                return;

            var admin = new User
            {
                Username = "admin",
                Role = "Admin",
                IsApproved = true,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin123")
            };

            context.Users.Add(admin);
            await context.SaveChangesAsync();
        }
    }
}