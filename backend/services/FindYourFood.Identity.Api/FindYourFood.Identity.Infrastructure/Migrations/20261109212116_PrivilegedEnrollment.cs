using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FindYourFood.Identity.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class PrivilegedEnrollment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PrivilegedEnrollments",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsOwner = table.Column<bool>(type: "bit", nullable: false),
                    ProtectedAuthenticator = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LastAcceptedStep = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PrivilegedEnrollments", x => x.UserId);
                    table.ForeignKey(
                        name: "FK_PrivilegedEnrollments_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PrivilegedEnrollments_IsOwner",
                table: "PrivilegedEnrollments",
                column: "IsOwner",
                unique: true,
                filter: "[IsOwner] = 1");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PrivilegedEnrollments");
        }
    }
}
