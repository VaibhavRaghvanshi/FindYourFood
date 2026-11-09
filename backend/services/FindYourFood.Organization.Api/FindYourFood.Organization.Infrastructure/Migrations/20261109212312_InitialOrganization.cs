using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FindYourFood.Organization.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialOrganization : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Grants",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SubjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Level = table.Column<int>(type: "int", nullable: false),
                    Permission = table.Column<string>(type: "varchar(64)", unicode: false, maxLength: 64, nullable: false),
                    Scope = table.Column<string>(type: "varchar(160)", unicode: false, maxLength: 160, nullable: false),
                    CanDelegate = table.Column<bool>(type: "bit", nullable: false),
                    ParentGrantId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    GrantedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GrantedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    RevokedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Grants", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Grants_Grants_ParentGrantId",
                        column: x => x.ParentGrantId,
                        principalTable: "Grants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Audit",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GrantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ActorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Action = table.Column<string>(type: "varchar(16)", unicode: false, maxLength: 16, nullable: false),
                    TraceId = table.Column<string>(type: "varchar(64)", unicode: false, maxLength: 64, nullable: false),
                    OccurredAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Audit", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Audit_Grants_GrantId",
                        column: x => x.GrantId,
                        principalTable: "Grants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Audit_GrantId",
                table: "Audit",
                column: "GrantId");

            migrationBuilder.CreateIndex(
                name: "IX_Grants_GrantedAt",
                table: "Grants",
                column: "GrantedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Grants_GrantedBy_GrantedAt",
                table: "Grants",
                columns: new[] { "GrantedBy", "GrantedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Grants_ParentGrantId",
                table: "Grants",
                column: "ParentGrantId");

            migrationBuilder.CreateIndex(
                name: "IX_Grants_SubjectId_Permission_Scope",
                table: "Grants",
                columns: new[] { "SubjectId", "Permission", "Scope" },
                unique: true,
                filter: "[RevokedAt] IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Audit");

            migrationBuilder.DropTable(
                name: "Grants");
        }
    }
}
