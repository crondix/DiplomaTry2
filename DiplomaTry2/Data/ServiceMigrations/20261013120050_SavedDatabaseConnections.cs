using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DiplomaTry2.Data.ServiceMigrations
{
    /// <inheritdoc />
    public partial class SavedDatabaseConnections : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DatabaseConnections",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ConnectionKey = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    ProtectedConnection = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DatabaseConnections", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DatabaseConnections_ConnectionKey",
                table: "DatabaseConnections",
                column: "ConnectionKey",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DatabaseConnections");
        }
    }
}
