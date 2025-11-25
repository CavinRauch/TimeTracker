using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Timetracker.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAppSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AppSettingsEntity",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PromptOnStop = table.Column<bool>(type: "INTEGER", nullable: false),
                    DefaultMetadataJson = table.Column<string>(type: "TEXT", nullable: true),
                    ModifiedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "BLOB", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppSettingsEntity", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "AppSettingsEntity",
                columns: new[] { "Id", "DefaultMetadataJson", "ModifiedAt", "PromptOnStop" },
                values: new object[] { 1, null, new DateTimeOffset(new DateTime(2001, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 2, 0, 0, 0)), true });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AppSettingsEntity");
        }
    }
}
