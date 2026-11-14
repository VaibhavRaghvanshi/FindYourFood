using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FindYourFood.Ordering.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class NativeMessaging : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "PaymentId",
                table: "Sagas",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "AggregateVersion",
                table: "Outbox",
                type: "bigint",
                nullable: false,
                defaultValue: 1L);

            migrationBuilder.AddColumn<int>(
                name: "Attempts",
                table: "Outbox",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "FailedAt",
                table: "Outbox",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastError",
                table: "Outbox",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "LeaseOwner",
                table: "Outbox",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LeaseUntil",
                table: "Outbox",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "NextAttemptAt",
                table: "Outbox",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Source",
                table: "Outbox",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "ordering");

            migrationBuilder.CreateTable(
                name: "Inbox",
                columns: table => new
                {
                    MessageId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Source = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Type = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ContentHash = table.Column<string>(type: "varchar(64)", unicode: false, maxLength: 64, nullable: false),
                    ProcessedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Inbox", x => x.MessageId);
                });

            migrationBuilder.CreateTable(
                name: "RejectedMessages",
                columns: table => new
                {
                    ContentHash = table.Column<string>(type: "varchar(64)", unicode: false, maxLength: 64, nullable: false),
                    BodyBase64 = table.Column<string>(type: "varchar(max)", unicode: false, maxLength: 90000, nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ReceivedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RejectedMessages", x => x.ContentHash);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Outbox_FailedAt_NextAttemptAt_LeaseUntil",
                table: "Outbox",
                columns: new[] { "FailedAt", "NextAttemptAt", "LeaseUntil" });

            // Preserve the revision of cancellation events written before messaging was enabled.
            migrationBuilder.Sql("UPDATE Outbox SET AggregateVersion = 2 WHERE Type IN ('ordering.order-cancelled.v1', 'ordering.payment-cancelled.v1');");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Inbox");

            migrationBuilder.DropTable(
                name: "RejectedMessages");

            migrationBuilder.DropIndex(
                name: "IX_Outbox_FailedAt_NextAttemptAt_LeaseUntil",
                table: "Outbox");

            migrationBuilder.DropColumn(
                name: "PaymentId",
                table: "Sagas");

            migrationBuilder.DropColumn(
                name: "AggregateVersion",
                table: "Outbox");

            migrationBuilder.DropColumn(
                name: "Attempts",
                table: "Outbox");

            migrationBuilder.DropColumn(
                name: "FailedAt",
                table: "Outbox");

            migrationBuilder.DropColumn(
                name: "LastError",
                table: "Outbox");

            migrationBuilder.DropColumn(
                name: "LeaseOwner",
                table: "Outbox");

            migrationBuilder.DropColumn(
                name: "LeaseUntil",
                table: "Outbox");

            migrationBuilder.DropColumn(
                name: "NextAttemptAt",
                table: "Outbox");

            migrationBuilder.DropColumn(
                name: "Source",
                table: "Outbox");
        }
    }
}
