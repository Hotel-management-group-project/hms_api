// Student ID: S2401276
// Student Name: Mohamed Iyaadh Ahmed
// Module: Advanced Software Development (UFCF8S-30-2)

using HMS.API.Models;

namespace HMS.API.Data.Seeders
{
    public static class AncillaryServiceSeeder
    {
        public static async Task SeedAsync(ApplicationDbContext db)
        {
            if (db.AncillaryServices.Any()) return;

            db.AncillaryServices.AddRange(
                new AncillaryService
                {
                    Name = "Airport Transfer",
                    Price = 50m,
                    Description = "One-way private transfer between the hotel and the nearest airport."
                },
                new AncillaryService
                {
                    Name = "Full English Breakfast",
                    Price = 20m,
                    Description = "Full cooked breakfast per person per day, served in the dining room from 07:00–10:30."
                },
                new AncillaryService
                {
                    Name = "Spa Access",
                    Price = 35m,
                    Description = "Full-day access to the hotel spa per person, including pool, sauna, and steam room."
                },
                new AncillaryService
                {
                    Name = "Late Check-out",
                    Price = 40m,
                    Description = "Extended check-out until 14:00, subject to availability."
                }
            );

            await db.SaveChangesAsync();
        }
    }
}
