using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectLucy.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPaymentTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "payment",
                columns: table => new
                {
                    payment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    order_invoice_number = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    order_amount = table.Column<long>(type: "bigint", nullable: false),
                    order_description = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    customer_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "PENDING"),
                    transaction_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("payment_pkey", x => x.payment_id);
                });

            migrationBuilder.CreateIndex(
                name: "idx_payment_invoice",
                table: "payment",
                column: "order_invoice_number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_payment_transaction_id",
                table: "payment",
                column: "transaction_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "payment");
        }
    }
}
