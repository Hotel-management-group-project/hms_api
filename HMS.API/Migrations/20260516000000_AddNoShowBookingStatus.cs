
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HMS.API.Migrations
{
    /// <inheritdoc />
    public partial class AddNoShowBookingStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // BookingStatus is stored as text — adding the NoShow variant requires no DDL change.
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
