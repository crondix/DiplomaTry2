using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DiplomaTry2.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PrinterModels",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ModelName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    NormalizedModelName = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    MaxCopyPerPrint = table.Column<short>(type: "smallint", nullable: true),
                    IsDuplexing = table.Column<bool>(type: "bit", nullable: true),
                    IsColor = table.Column<bool>(type: "bit", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PrinterModels", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PrintserverEvents",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<short>(type: "smallint", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PrintserverEvents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Sender",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    NameNormalized = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sender", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SentPrintingFile",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Size = table.Column<int>(type: "int", nullable: false),
                    Pages = table.Column<short>(type: "smallint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SentPrintingFile", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "NetworkPrinters",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PrinterModelId = table.Column<int>(type: "int", nullable: true),
                    ShareName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Comment = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Ip = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NetworkPrinters", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NetworkPrinters_PrinterModels_PrinterModelId",
                        column: x => x.PrinterModelId,
                        principalTable: "PrinterModels",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "PaperSizes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Width = table.Column<double>(type: "float", nullable: true),
                    Height = table.Column<double>(type: "float", nullable: true),
                    PrinterModelId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaperSizes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PaperSizes_PrinterModels_PrinterModelId",
                        column: x => x.PrinterModelId,
                        principalTable: "PrinterModels",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "SenderDevice",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    NameNormalized = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    NetworkPrinterId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SenderDevice", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SenderDevice_NetworkPrinters_NetworkPrinterId",
                        column: x => x.NetworkPrinterId,
                        principalTable: "NetworkPrinters",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "PrintEvents",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DateTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SenderId = table.Column<int>(type: "int", nullable: true),
                    SenderDeviceId = table.Column<int>(type: "int", nullable: true),
                    SentPrintingFileId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PrintEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PrintEvents_SenderDevice_SenderDeviceId",
                        column: x => x.SenderDeviceId,
                        principalTable: "SenderDevice",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PrintEvents_Sender_SenderId",
                        column: x => x.SenderId,
                        principalTable: "Sender",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PrintEvents_SentPrintingFile_SentPrintingFileId",
                        column: x => x.SentPrintingFileId,
                        principalTable: "SentPrintingFile",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_NetworkPrinters_PrinterModelId",
                table: "NetworkPrinters",
                column: "PrinterModelId");

            migrationBuilder.CreateIndex(
                name: "IX_PaperSizes_Name",
                table: "PaperSizes",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PaperSizes_PrinterModelId",
                table: "PaperSizes",
                column: "PrinterModelId");

            migrationBuilder.CreateIndex(
                name: "IX_PrinterModels_NormalizedModelName",
                table: "PrinterModels",
                column: "NormalizedModelName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PrintEvents_SenderDeviceId",
                table: "PrintEvents",
                column: "SenderDeviceId");

            migrationBuilder.CreateIndex(
                name: "IX_PrintEvents_SenderId",
                table: "PrintEvents",
                column: "SenderId");

            migrationBuilder.CreateIndex(
                name: "IX_PrintEvents_SentPrintingFileId",
                table: "PrintEvents",
                column: "SentPrintingFileId");

            migrationBuilder.CreateIndex(
                name: "IX_SenderDevice_NetworkPrinterId",
                table: "SenderDevice",
                column: "NetworkPrinterId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PaperSizes");

            migrationBuilder.DropTable(
                name: "PrintEvents");

            migrationBuilder.DropTable(
                name: "PrintserverEvents");

            migrationBuilder.DropTable(
                name: "SenderDevice");

            migrationBuilder.DropTable(
                name: "Sender");

            migrationBuilder.DropTable(
                name: "SentPrintingFile");

            migrationBuilder.DropTable(
                name: "NetworkPrinters");

            migrationBuilder.DropTable(
                name: "PrinterModels");
        }
    }
}
