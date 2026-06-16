using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectLucy.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SwitchPaymentToTransactions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // SePay payments now live in the existing "transactions" table instead of
            // a dedicated "payment" table.
            migrationBuilder.DropTable(
                name: "payment");

            // transactions.user_id: int -> uuid, linked to the user table.
            // The table is empty, so the type change carries no data-loss risk.
            migrationBuilder.Sql(@"ALTER TABLE transactions ALTER COLUMN user_id TYPE uuid USING (NULL::uuid);");
            migrationBuilder.Sql(@"ALTER TABLE transactions
                ADD CONSTRAINT fk_transactions_user
                FOREIGN KEY (user_id) REFERENCES ""user""(user_id);");

            // Seed the transaction type used by CreatePaymentCommandHandler.
            migrationBuilder.Sql(@"INSERT INTO transaction_type (code, name, description, is_active)
                VALUES ('ONLINE_SEPAY', 'SePay Online Payment', 'Payment made online through the SePay gateway', true)
                ON CONFLICT (code) DO NOTHING;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"DELETE FROM transaction_type WHERE code = 'ONLINE_SEPAY';");
            migrationBuilder.Sql(@"ALTER TABLE transactions DROP CONSTRAINT IF EXISTS fk_transactions_user;");
            migrationBuilder.Sql(@"ALTER TABLE transactions ALTER COLUMN user_id TYPE integer USING (0);");

            migrationBuilder.CreateTable(
                name: "payment",
                columns: table => new
                {
                    payment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    customer_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    order_amount = table.Column<long>(type: "bigint", nullable: false),
                    order_description = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    order_invoice_number = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "PENDING"),
                    transaction_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
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
    }
}
