using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ProjectLucy.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class FixWalletEntryTypeColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "gift_catalog",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    icon_url = table.Column<string>(type: "text", nullable: true),
                    price = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    currency = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false, defaultValueSql: "'VND'::character varying"),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("gift_catalog_pkey", x => x.id);
                },
                comment: "Danh mục quà ảo có thể tặng trong phòng học. is_active = FALSE để ẩn khỏi UI mà không xóa lịch sử.");

            migrationBuilder.CreateTable(
                name: "payment_method",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    transaction_id = table.Column<long>(type: "bigint", nullable: false),
                    provider = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    provider_txn_id = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    raw_status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    metadata = table.Column<string>(type: "jsonb", nullable: true),
                    paid_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("payment_method_pkey", x => x.id);
                    table.ForeignKey(
                        name: "fk_payment_method_transaction",
                        column: x => x.transaction_id,
                        principalTable: "transactions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                },
                comment: "Lưu chi tiết từ cổng thanh toán (VNPAY/Momo/ZaloPay...). metadata giữ nguyên payload gốc để debug & đối soát.");

            migrationBuilder.CreateTable(
                name: "role_upgrade_order",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    transaction_id = table.Column<long>(type: "bigint", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    from_role_id = table.Column<int>(type: "integer", nullable: false),
                    to_role_id = table.Column<int>(type: "integer", nullable: false),
                    role_price_id = table.Column<int>(type: "integer", nullable: false),
                    activated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    cancelled_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("role_upgrade_order_pkey", x => x.id);
                    table.ForeignKey(
                        name: "fk_upgrade_from_role",
                        column: x => x.from_role_id,
                        principalTable: "role",
                        principalColumn: "role_id");
                    table.ForeignKey(
                        name: "fk_upgrade_role_price",
                        column: x => x.role_price_id,
                        principalTable: "role_price",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_upgrade_to_role",
                        column: x => x.to_role_id,
                        principalTable: "role",
                        principalColumn: "role_id");
                    table.ForeignKey(
                        name: "fk_upgrade_transaction",
                        column: x => x.transaction_id,
                        principalTable: "transactions",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_upgrade_user",
                        column: x => x.user_id,
                        principalTable: "user",
                        principalColumn: "user_id");
                },
                comment: "Đơn hàng nâng cấp tài khoản (Pro/Super). activated_at được set khi transaction → completed. expires_at = activated_at + role_price.duration (NULL = không hết hạn).");

            migrationBuilder.CreateTable(
                name: "wallet",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    balance = table.Column<decimal>(type: "numeric(15,2)", precision: 15, scale: 2, nullable: false, defaultValueSql: "0.00"),
                    currency = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false, defaultValueSql: "'VND'::character varying"),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("wallet_pkey", x => x.id);
                    table.ForeignKey(
                        name: "fk_wallet_user",
                        column: x => x.user_id,
                        principalTable: "user",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Cascade);
                },
                comment: "Ví điện tử 1-1 với user. Balance luôn >= 0, mọi thay đổi phải qua wallet_ledger.");

            migrationBuilder.CreateTable(
                name: "gift_transaction",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    transaction_id = table.Column<long>(type: "bigint", nullable: false),
                    sender_id = table.Column<Guid>(type: "uuid", nullable: false),
                    receiver_id = table.Column<Guid>(type: "uuid", nullable: false),
                    gift_id = table.Column<Guid>(type: "uuid", nullable: false),
                    room_session_id = table.Column<Guid>(type: "uuid", nullable: true),
                    quantity = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    total_value = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("gift_transaction_pkey", x => x.id);
                    table.ForeignKey(
                        name: "fk_gift_txn_gift",
                        column: x => x.gift_id,
                        principalTable: "gift_catalog",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_gift_txn_receiver",
                        column: x => x.receiver_id,
                        principalTable: "user",
                        principalColumn: "user_id");
                    table.ForeignKey(
                        name: "fk_gift_txn_sender",
                        column: x => x.sender_id,
                        principalTable: "user",
                        principalColumn: "user_id");
                    table.ForeignKey(
                        name: "fk_gift_txn_transaction",
                        column: x => x.transaction_id,
                        principalTable: "transactions",
                        principalColumn: "id");
                },
                comment: "Chi tiết tặng quà. transaction_id trỏ đến bản ghi DEBIT trong transactions. Luồng: sender ví DEBIT → wallet_ledger → gift_transaction. Receiver nhận CREDIT riêng qua transaction mới.");

            migrationBuilder.CreateTable(
                name: "wallet_ledger",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    wallet_id = table.Column<Guid>(type: "uuid", nullable: false),
                    transaction_id = table.Column<long>(type: "bigint", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(15,2)", precision: 15, scale: 2, nullable: false),
                    balance_after = table.Column<decimal>(type: "numeric(15,2)", precision: 15, scale: 2, nullable: false),
                    entry_type = table.Column<string>(type: "text", nullable: false),
                    note = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("wallet_ledger_pkey", x => x.id);
                    table.ForeignKey(
                        name: "fk_ledger_transaction",
                        column: x => x.transaction_id,
                        principalTable: "transactions",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_ledger_wallet",
                        column: x => x.wallet_id,
                        principalTable: "wallet",
                        principalColumn: "id");
                },
                comment: "Sổ cái bất biến. Không UPDATE/DELETE — chỉ INSERT. Dùng để audit, đối soát, và tái tính balance nếu cần.");

            migrationBuilder.CreateIndex(
                name: "idx_gift_catalog_active",
                table: "gift_catalog",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "gift_transaction_transaction_id_key",
                table: "gift_transaction",
                column: "transaction_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_gift_txn_receiver",
                table: "gift_transaction",
                columns: new[] { "receiver_id", "created_at" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "idx_gift_txn_sender",
                table: "gift_transaction",
                columns: new[] { "sender_id", "created_at" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "idx_gift_txn_session",
                table: "gift_transaction",
                column: "room_session_id");

            migrationBuilder.CreateIndex(
                name: "IX_gift_transaction_gift_id",
                table: "gift_transaction",
                column: "gift_id");

            migrationBuilder.CreateIndex(
                name: "idx_payment_method_provider",
                table: "payment_method",
                column: "provider");

            migrationBuilder.CreateIndex(
                name: "idx_payment_method_provider_txn",
                table: "payment_method",
                column: "provider_txn_id");

            migrationBuilder.CreateIndex(
                name: "payment_method_transaction_id_key",
                table: "payment_method",
                column: "transaction_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_upgrade_active",
                table: "role_upgrade_order",
                columns: new[] { "user_id", "expires_at" },
                filter: "((activated_at IS NOT NULL) AND (cancelled_at IS NULL))");

            migrationBuilder.CreateIndex(
                name: "idx_upgrade_user",
                table: "role_upgrade_order",
                columns: new[] { "user_id", "created_at" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "IX_role_upgrade_order_from_role_id",
                table: "role_upgrade_order",
                column: "from_role_id");

            migrationBuilder.CreateIndex(
                name: "IX_role_upgrade_order_role_price_id",
                table: "role_upgrade_order",
                column: "role_price_id");

            migrationBuilder.CreateIndex(
                name: "IX_role_upgrade_order_to_role_id",
                table: "role_upgrade_order",
                column: "to_role_id");

            migrationBuilder.CreateIndex(
                name: "role_upgrade_order_transaction_id_key",
                table: "role_upgrade_order",
                column: "transaction_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "wallet_user_id_key",
                table: "wallet",
                column: "user_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_wallet_ledger_txn",
                table: "wallet_ledger",
                column: "transaction_id");

            migrationBuilder.CreateIndex(
                name: "idx_wallet_ledger_wallet",
                table: "wallet_ledger",
                columns: new[] { "wallet_id", "created_at" },
                descending: new[] { false, true });

            migrationBuilder.AddForeignKey(
                name: "fk_transactions_user",
                table: "transactions",
                column: "user_id",
                principalTable: "user",
                principalColumn: "user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_transactions_user",
                table: "transactions");

            migrationBuilder.DropTable(
                name: "gift_transaction");

            migrationBuilder.DropTable(
                name: "payment_method");

            migrationBuilder.DropTable(
                name: "role_upgrade_order");

            migrationBuilder.DropTable(
                name: "wallet_ledger");

            migrationBuilder.DropTable(
                name: "gift_catalog");

            migrationBuilder.DropTable(
                name: "wallet");
        }
    }
}
