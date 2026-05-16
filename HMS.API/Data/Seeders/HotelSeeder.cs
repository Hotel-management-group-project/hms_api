
using HMS.API.Models;

namespace HMS.API.Data.Seeders
{
    public static class HotelSeeder
    {
        public static async Task SeedAsync(ApplicationDbContext db)
        {
            if (db.Hotels.Any()) return;

            db.Hotels.AddRange(
                new Hotel
                {
                    Name = "The Grand London",
                    Location = "London, UK",
                    Address = "15 Mayfair Boulevard, London, W1K 4PL, United Kingdom",
                    Description = "A timeless five-star retreat in the heart of Mayfair, blending classic British elegance with modern luxury. Steps from Hyde Park and the finest boutiques.",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                },
                new Hotel
                {
                    Name = "Everblue Resort",
                    Location = "Maldives",
                    Address = "North Malé Atoll, Kaafu, Maldives",
                    Description = "An overwater paradise surrounded by crystal-clear lagoons. Private villas with direct ocean access, world-class diving, and secluded beach dining.",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                },
                new Hotel
                {
                    Name = "Maison Paris",
                    Location = "Paris, France",
                    Address = "42 Avenue des Champs-Élysées, 75008 Paris, France",
                    Description = "Intimate Haussmann-era boutique hotel steps from the Arc de Triomphe. Bespoke interiors, Michelin-starred dining, and panoramic city views.",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                }
            );

            await db.SaveChangesAsync();
        }
    }
}
