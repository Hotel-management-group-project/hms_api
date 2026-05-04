// Student ID: S2401276
// Student Name: Mohamed Iyaadh Ahmed
// Module: Advanced Software Development (UFCF8S-30-2)

using HMS.API.Models;
using Microsoft.AspNetCore.Identity;

namespace HMS.API.Data.Seeders
{
    public static class UserSeeder
    {
        private static readonly (string Email, string Password, string FirstName, string LastName, string Role)[] Seeds =
        [
            ("admin@hms.com",     "Admin@123456",     "Alice",   "Admin",    "Admin"),
            ("manager@hms.com",   "Manager@123456",   "Marcus",  "Manager",  "Manager"),
            ("frontdesk@hms.com", "FrontDesk@123456", "Fiona",   "Desk",     "FrontDesk"),
            ("guest@hms.com",     "Guest@123456",     "George",  "Guest",    "Guest"),
        ];

        public static async Task SeedAsync(
            UserManager<User> userManager,
            RoleManager<IdentityRole> roleManager)
        {
            foreach (var (email, password, firstName, lastName, role) in Seeds)
            {
                if (await userManager.FindByEmailAsync(email) is not null)
                    continue;

                var user = new User
                {
                    UserName = email,
                    Email = email,
                    FirstName = firstName,
                    LastName = lastName,
                    Role = role,
                    IsActive = true,
                    EmailConfirmed = true,
                    CreatedAt = DateTime.UtcNow
                };

                var result = await userManager.CreateAsync(user, password);
                if (!result.Succeeded)
                    throw new Exception(
                        $"UserSeeder failed for {email}: {string.Join(", ", result.Errors.Select(e => e.Description))}");

                await userManager.AddToRoleAsync(user, role);
            }
        }
    }
}
