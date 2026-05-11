using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PullUp.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddRsvpReminderSentAt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ReminderSentAt",
                table: "Rsvps",
                type: "datetimeoffset",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ReminderSentAt",
                table: "Rsvps");
        }
    }
}
