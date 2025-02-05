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
                name: "AspNetRoles",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUsers",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    UserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedUserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    EmailConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SecurityStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhoneNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhoneNumberConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    TwoFactorEnabled = table.Column<bool>(type: "bit", nullable: false),
                    LockoutEnd = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LockoutEnabled = table.Column<bool>(type: "bit", nullable: false),
                    AccessFailedCount = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUsers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ChangeLogs",
                columns: table => new
                {
                    TableName = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    LastModified = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChangeLogs", x => x.TableName);
                });

            migrationBuilder.CreateTable(
                name: "DocumentNames",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    name = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentNames", x => x.id);
                });

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
                name: "SenderDevices",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    NameNormalized = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SenderDevices", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Senders",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    NameNormalized = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Senders", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetRoleClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoleId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ClaimType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClaimValue = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoleClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetRoleClaims_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ClaimType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClaimValue = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetUserClaims_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserLogins",
                columns: table => new
                {
                    LoginProvider = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ProviderKey = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ProviderDisplayName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserLogins", x => new { x.LoginProvider, x.ProviderKey });
                    table.ForeignKey(
                        name: "FK_AspNetUserLogins_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserRoles",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    RoleId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserRoles", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserTokens",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    LoginProvider = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Value = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserTokens", x => new { x.UserId, x.LoginProvider, x.Name });
                    table.ForeignKey(
                        name: "FK_AspNetUserTokens_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SentPrintingFiles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nameid = table.Column<int>(type: "int", nullable: false),
                    Size = table.Column<long>(type: "bigint", nullable: false),
                    Pages = table.Column<short>(type: "smallint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SentPrintingFiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SentPrintingFiles_DocumentNames_Nameid",
                        column: x => x.Nameid,
                        principalTable: "DocumentNames",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "NetworkPrinters",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PrinterModelId = table.Column<int>(type: "int", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ShareName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Comment = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Ip = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NonIPAddress = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsOnline = table.Column<bool>(type: "bit", nullable: false)
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
                name: "TargetPrinters",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    NameNormalized = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    NetworkPrinterId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TargetPrinters", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TargetPrinters_NetworkPrinters_NetworkPrinterId",
                        column: x => x.NetworkPrinterId,
                        principalTable: "NetworkPrinters",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "EventsSuccessfulPrinting",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DateTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SenderId = table.Column<int>(type: "int", nullable: true),
                    SenderDeviceId = table.Column<int>(type: "int", nullable: true),
                    TargetPrinterId = table.Column<int>(type: "int", nullable: true),
                    SentPrintingFileId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventsSuccessfulPrinting", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EventsSuccessfulPrinting_SenderDevices_SenderDeviceId",
                        column: x => x.SenderDeviceId,
                        principalTable: "SenderDevices",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_EventsSuccessfulPrinting_Senders_SenderId",
                        column: x => x.SenderId,
                        principalTable: "Senders",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_EventsSuccessfulPrinting_SentPrintingFiles_SentPrintingFileId",
                        column: x => x.SentPrintingFileId,
                        principalTable: "SentPrintingFiles",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_EventsSuccessfulPrinting_TargetPrinters_TargetPrinterId",
                        column: x => x.TargetPrinterId,
                        principalTable: "TargetPrinters",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_AspNetRoleClaims_RoleId",
                table: "AspNetRoleClaims",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                table: "AspNetRoles",
                column: "NormalizedName",
                unique: true,
                filter: "[NormalizedName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserClaims_UserId",
                table: "AspNetUserClaims",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserLogins_UserId",
                table: "AspNetUserLogins",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserRoles_RoleId",
                table: "AspNetUserRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                table: "AspNetUsers",
                column: "NormalizedEmail");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                table: "AspNetUsers",
                column: "NormalizedUserName",
                unique: true,
                filter: "[NormalizedUserName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentNames_name",
                table: "DocumentNames",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EventsSuccessfulPrinting_SenderDeviceId",
                table: "EventsSuccessfulPrinting",
                column: "SenderDeviceId");

            migrationBuilder.CreateIndex(
                name: "IX_EventsSuccessfulPrinting_SenderId",
                table: "EventsSuccessfulPrinting",
                column: "SenderId");

            migrationBuilder.CreateIndex(
                name: "IX_EventsSuccessfulPrinting_SentPrintingFileId",
                table: "EventsSuccessfulPrinting",
                column: "SentPrintingFileId");

            migrationBuilder.CreateIndex(
                name: "IX_EventsSuccessfulPrinting_TargetPrinterId",
                table: "EventsSuccessfulPrinting",
                column: "TargetPrinterId");

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
                name: "IX_PrintserverEvents_Code",
                table: "PrintserverEvents",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SenderDevices_NameNormalized",
                table: "SenderDevices",
                column: "NameNormalized",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Senders_NameNormalized",
                table: "Senders",
                column: "NameNormalized",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SentPrintingFiles_Nameid",
                table: "SentPrintingFiles",
                column: "Nameid");

            migrationBuilder.CreateIndex(
                name: "IX_TargetPrinters_NameNormalized",
                table: "TargetPrinters",
                column: "NameNormalized",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TargetPrinters_NetworkPrinterId",
                table: "TargetPrinters",
                column: "NetworkPrinterId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AspNetRoleClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserLogins");

            migrationBuilder.DropTable(
                name: "AspNetUserRoles");

            migrationBuilder.DropTable(
                name: "AspNetUserTokens");

            migrationBuilder.DropTable(
                name: "ChangeLogs");

            migrationBuilder.DropTable(
                name: "EventsSuccessfulPrinting");

            migrationBuilder.DropTable(
                name: "PaperSizes");

            migrationBuilder.DropTable(
                name: "PrintserverEvents");

            migrationBuilder.DropTable(
                name: "AspNetRoles");

            migrationBuilder.DropTable(
                name: "AspNetUsers");

            migrationBuilder.DropTable(
                name: "SenderDevices");

            migrationBuilder.DropTable(
                name: "Senders");

            migrationBuilder.DropTable(
                name: "SentPrintingFiles");

            migrationBuilder.DropTable(
                name: "TargetPrinters");

            migrationBuilder.DropTable(
                name: "DocumentNames");

            migrationBuilder.DropTable(
                name: "NetworkPrinters");

            migrationBuilder.DropTable(
                name: "PrinterModels");
        }
    }
}
