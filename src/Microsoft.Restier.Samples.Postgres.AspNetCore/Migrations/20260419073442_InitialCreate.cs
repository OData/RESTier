using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Microsoft.Restier.Samples.Postgres.AspNetCore.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:uuid-ossp", ",,");

            migrationBuilder.CreateTable(
                name: "UserTypes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    DisplayName = table.Column<string>(type: "text", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: true),
                    DateCreated = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserTypes_Id", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EmailAddress = table.Column<string>(type: "text", nullable: true),
                    UserTypeId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users_Id", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Users_UserTypes",
                        column: x => x.UserTypeId,
                        principalTable: "UserTypes",
                        principalColumn: "Id");
                });

            migrationBuilder.InsertData(
                table: "UserTypes",
                columns: new[] { "Id", "DateCreated", "DisplayName", "IsActive" },
                values: new object[,]
                {
                    { new Guid("a1b2c3d4-0001-0001-0001-000000000001"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Administrator", true },
                    { new Guid("a1b2c3d4-0001-0001-0001-000000000002"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Editor", true },
                    { new Guid("a1b2c3d4-0001-0001-0001-000000000003"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Viewer", true }
                });

            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "Id", "EmailAddress", "UserTypeId" },
                values: new object[,]
                {
                    { new Guid("b2c3d4e5-0002-0002-0002-000000000001"), "admin@example.com", new Guid("a1b2c3d4-0001-0001-0001-000000000001") },
                    { new Guid("b2c3d4e5-0002-0002-0002-000000000002"), "editor@example.com", new Guid("a1b2c3d4-0001-0001-0001-000000000002") },
                    { new Guid("b2c3d4e5-0002-0002-0002-000000000003"), "viewer@example.com", new Guid("a1b2c3d4-0001-0001-0001-000000000003") },
                    { new Guid("b2c3d4e5-0002-0002-0002-000000000004"), "another.admin@example.com", new Guid("a1b2c3d4-0001-0001-0001-000000000001") }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Users_UserTypeId",
                table: "Users",
                column: "UserTypeId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "UserTypes");
        }
    }
}
