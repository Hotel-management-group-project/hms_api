
using HMS.API.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace HMS.API.Data
{
    public class ApplicationDbContext : IdentityDbContext<User>
    {
        private readonly string? _encryptionKey;

        public ApplicationDbContext(
            DbContextOptions<ApplicationDbContext> options,
            IConfiguration config) : base(options)
        {
            _encryptionKey = config["DataProtection:EncryptionKey"];
        }

        public DbSet<Hotel> Hotels => Set<Hotel>();
        public DbSet<Room> Rooms => Set<Room>();
        public DbSet<Booking> Bookings => Set<Booking>();
        public DbSet<BookingRoom> BookingRooms => Set<BookingRoom>();
        public DbSet<Payment> Payments => Set<Payment>();
        public DbSet<AncillaryService> AncillaryServices => Set<AncillaryService>();
        public DbSet<BookingAncillaryService> BookingAncillaryServices => Set<BookingAncillaryService>();
        public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
        public DbSet<Waitlist> Waitlists => Set<Waitlist>();

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Encrypt sensitive fields at rest when a key is configured
            if (!string.IsNullOrEmpty(_encryptionKey))
            {
                var converter = new EncryptedStringConverter(_encryptionKey);

                builder.Entity<User>()
                    .Property(u => u.PhoneNumber)
                    .HasConversion(converter);

                builder.Entity<Payment>()
                    .Property(p => p.TransactionReference)
                    .HasConversion(converter);
            }

            builder.Entity<Hotel>(e =>
            {
                e.HasKey(h => h.Id);
                e.Property(h => h.Name).IsRequired().HasMaxLength(200);
                e.Property(h => h.Location).IsRequired().HasMaxLength(200);
                e.Property(h => h.Address).IsRequired().HasMaxLength(500);
                e.Property(h => h.Description).IsRequired();
                e.Property(h => h.ImageUrl).HasMaxLength(500);
            });

            builder.Entity<Room>(e =>
            {
                e.HasKey(r => r.Id);
                e.Property(r => r.RoomNumber).IsRequired().HasMaxLength(10);
                e.Property(r => r.Type).HasConversion<string>();
                e.Property(r => r.Status).HasConversion<string>();
                e.Property(r => r.PriceOffPeak).HasColumnType("decimal(10,2)");
                e.Property(r => r.PricePeak).HasColumnType("decimal(10,2)");
                e.HasOne(r => r.Hotel)
                    .WithMany(h => h.Rooms)
                    .HasForeignKey(r => r.HotelId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            builder.Entity<Booking>(e =>
            {
                e.HasKey(b => b.Id);
                e.Property(b => b.ReferenceNumber).IsRequired().HasMaxLength(20);
                e.HasIndex(b => b.ReferenceNumber).IsUnique();
                e.Property(b => b.Status).HasConversion<string>();
                e.Property(b => b.TotalPrice).HasColumnType("decimal(10,2)");
                e.Property(b => b.CancellationFee).HasColumnType("decimal(10,2)");
                e.HasOne(b => b.Guest)
                    .WithMany(u => u.Bookings)
                    .HasForeignKey(b => b.GuestId)
                    .OnDelete(DeleteBehavior.Restrict);
                e.HasOne(b => b.Hotel)
                    .WithMany()
                    .HasForeignKey(b => b.HotelId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<BookingRoom>(e =>
            {
                e.HasKey(br => br.Id);
                e.HasOne(br => br.Booking)
                    .WithMany(b => b.BookingRooms)
                    .HasForeignKey(br => br.BookingId)
                    .OnDelete(DeleteBehavior.Cascade);
                e.HasOne(br => br.Room)
                    .WithMany(r => r.BookingRooms)
                    .HasForeignKey(br => br.RoomId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<Payment>(e =>
            {
                e.HasKey(p => p.Id);
                e.Property(p => p.Amount).HasColumnType("decimal(10,2)");
                e.Property(p => p.Status).HasConversion<string>();
                e.HasOne(p => p.Booking)
                    .WithOne(b => b.Payment)
                    .HasForeignKey<Payment>(p => p.BookingId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            builder.Entity<AncillaryService>(e =>
            {
                e.HasKey(a => a.Id);
                e.Property(a => a.Name).IsRequired().HasMaxLength(200);
                e.Property(a => a.Price).HasColumnType("decimal(10,2)");
            });

            builder.Entity<BookingAncillaryService>(e =>
            {
                e.HasKey(bas => bas.Id);
                e.Property(bas => bas.TotalPrice).HasColumnType("decimal(10,2)");
                e.HasOne(bas => bas.Booking)
                    .WithMany(b => b.BookingAncillaryServices)
                    .HasForeignKey(bas => bas.BookingId)
                    .OnDelete(DeleteBehavior.Cascade);
                e.HasOne(bas => bas.AncillaryService)
                    .WithMany(a => a.BookingAncillaryServices)
                    .HasForeignKey(bas => bas.AncillaryServiceId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<AuditLog>(e =>
            {
                e.HasKey(a => a.Id);
                e.Property(a => a.Action).IsRequired().HasMaxLength(100);
                e.Property(a => a.EntityType).IsRequired().HasMaxLength(100);
                e.HasOne(a => a.User)
                    .WithMany(u => u.AuditLogs)
                    .HasForeignKey(a => a.UserId)
                    .OnDelete(DeleteBehavior.SetNull)
                    .IsRequired(false);
            });

            builder.Entity<Waitlist>(e =>
            {
                e.HasKey(w => w.Id);
                e.Property(w => w.RoomType).HasConversion<string>();
                e.Property(w => w.Status).HasMaxLength(50);
                e.HasOne(w => w.Guest)
                    .WithMany()
                    .HasForeignKey(w => w.GuestId)
                    .OnDelete(DeleteBehavior.Cascade);
                e.HasOne(w => w.Hotel)
                    .WithMany()
                    .HasForeignKey(w => w.HotelId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}
