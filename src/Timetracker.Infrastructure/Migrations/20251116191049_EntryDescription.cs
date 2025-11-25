using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Timetracker.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class EntryDescription : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EntryDescriptions",
                columns: table => new
                {
                    TimeEntryId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    JsonData = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EntryDescriptions", x => x.TimeEntryId);
                    table.ForeignKey(
                        name: "FK_EntryDescriptions_TimeEntries_TimeEntryId",
                        column: x => x.TimeEntryId,
                        principalTable: "TimeEntries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EntryDescriptions_TimeEntryId",
                table: "EntryDescriptions",
                column: "TimeEntryId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EntryDescriptions");
        }
    }
}
