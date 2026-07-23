using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectLucy.Infrastructure.Persistence.Migrations;

[DbContext(typeof(NeonDbContext))]
[Migration("20260723050000_AddGiftIdempotencyAndImages")]
public partial class AddGiftIdempotencyAndImages : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            ALTER TABLE gift_transaction
                ADD COLUMN IF NOT EXISTS idempotency_key uuid;

            CREATE UNIQUE INDEX IF NOT EXISTS ux_gift_transaction_idempotency_key
                ON gift_transaction (idempotency_key)
                WHERE idempotency_key IS NOT NULL;

            UPDATE gift_catalog SET icon_url = 'https://cdn.jsdelivr.net/gh/twitter/twemoji@latest/assets/72x72/1f338.png', updated_at = now() WHERE name = 'Flower';
            UPDATE gift_catalog SET icon_url = 'https://cdn.jsdelivr.net/gh/twitter/twemoji@latest/assets/72x72/2b50.png', updated_at = now() WHERE name = 'Star';
            UPDATE gift_catalog SET icon_url = 'https://cdn.jsdelivr.net/gh/twitter/twemoji@latest/assets/72x72/1f3c6.png', updated_at = now() WHERE name = 'Trophy';
            UPDATE gift_catalog SET icon_url = 'https://cdn.jsdelivr.net/gh/twitter/twemoji@latest/assets/72x72/1f451.png', updated_at = now() WHERE name = 'Crown';
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("DROP INDEX IF EXISTS ux_gift_transaction_idempotency_key;");
        migrationBuilder.DropColumn(name: "idempotency_key", table: "gift_transaction");
    }
}
