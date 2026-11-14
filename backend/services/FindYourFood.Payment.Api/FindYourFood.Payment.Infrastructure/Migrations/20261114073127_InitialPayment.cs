using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FindYourFood.Payment.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialPayment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CancelledOrder",
                columns: table => new
                {
                    OrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OccurredAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CancelledOrder", x => x.OrderId);
                });

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
                name: "Intents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    GuestKeyHash = table.Column<string>(type: "varchar(64)", unicode: false, maxLength: 64, nullable: true),
                    TotalMinor = table.Column<long>(type: "bigint", nullable: false),
                    Currency = table.Column<string>(type: "varchar(3)", unicode: false, maxLength: 3, nullable: false),
                    Deadline = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Revision = table.Column<long>(type: "bigint", nullable: false),
                    State = table.Column<int>(type: "int", nullable: false),
                    ActiveAttemptId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SuccessfulPaymentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CancellationRequested = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Intents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Outbox",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Source = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false, defaultValue: ""),
                    Type = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    OrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AggregateVersion = table.Column<long>(type: "bigint", nullable: false, defaultValue: 1L),
                    PayloadJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OccurredAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    PublishedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LeaseOwner = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LeaseUntil = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    NextAttemptAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    FailedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    Attempts = table.Column<int>(type: "int", nullable: false),
                    LastError = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Outbox", x => x.Id);
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

            migrationBuilder.CreateTable(
                name: "PaymentAttempt",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IntentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IdempotencyKey = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SimulationSuccess = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CompletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ProviderReference = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    State = table.Column<int>(type: "int", nullable: false),
                    Revision = table.Column<long>(type: "bigint", nullable: false),
                    NextRetryAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    Attempts = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentAttempt", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PaymentAttempt_Intents_IntentId",
                        column: x => x.IntentId,
                        principalTable: "Intents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RefundInstruction",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PaymentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TotalMinor = table.Column<long>(type: "bigint", nullable: false),
                    Currency = table.Column<string>(type: "varchar(3)", unicode: false, maxLength: 3, nullable: false),
                    Completed = table.Column<bool>(type: "bit", nullable: false),
                    ProviderReference = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Revision = table.Column<long>(type: "bigint", nullable: false),
                    NextRetryAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    Attempts = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RefundInstruction", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RefundInstruction_PaymentAttempt_PaymentId",
                        column: x => x.PaymentId,
                        principalTable: "PaymentAttempt",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Intents_OrderId",
                table: "Intents",
                column: "OrderId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Outbox_FailedAt_NextAttemptAt_LeaseUntil",
                table: "Outbox",
                columns: new[] { "FailedAt", "NextAttemptAt", "LeaseUntil" });

            migrationBuilder.CreateIndex(
                name: "IX_Outbox_PublishedAt_OccurredAt",
                table: "Outbox",
                columns: new[] { "PublishedAt", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_PaymentAttempt_IntentId_IdempotencyKey",
                table: "PaymentAttempt",
                columns: new[] { "IntentId", "IdempotencyKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PaymentAttempt_State_NextRetryAt",
                table: "PaymentAttempt",
                columns: new[] { "State", "NextRetryAt" });

            migrationBuilder.CreateIndex(
                name: "IX_RefundInstruction_Completed_NextRetryAt",
                table: "RefundInstruction",
                columns: new[] { "Completed", "NextRetryAt" });

            migrationBuilder.CreateIndex(
                name: "IX_RefundInstruction_PaymentId",
                table: "RefundInstruction",
                column: "PaymentId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CancelledOrder");

            migrationBuilder.DropTable(
                name: "Inbox");

            migrationBuilder.DropTable(
                name: "Outbox");

            migrationBuilder.DropTable(
                name: "RefundInstruction");

            migrationBuilder.DropTable(
                name: "RejectedMessages");

            migrationBuilder.DropTable(
                name: "PaymentAttempt");

            migrationBuilder.DropTable(
                name: "Intents");
        }
    }
}
