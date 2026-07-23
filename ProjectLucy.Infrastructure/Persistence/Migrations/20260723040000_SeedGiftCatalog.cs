using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectLucy.Infrastructure.Persistence.Migrations;

[DbContext(typeof(NeonDbContext))]
[Migration("20260723040000_SeedGiftCatalog")]
public partial class SeedGiftCatalog : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            INSERT INTO gift_catalog (id, name, description, icon_url, price, currency, is_active, created_at, updated_at)
            SELECT gen_random_uuid(), 'Flower', 'A small thank-you for your mentor.', NULL, 10000.00, 'VND', true, now(), now()
            WHERE NOT EXISTS (SELECT 1 FROM gift_catalog WHERE name = 'Flower');

            INSERT INTO gift_catalog (id, name, description, icon_url, price, currency, is_active, created_at, updated_at)
            SELECT gen_random_uuid(), 'Star', 'Celebrate a helpful learning moment.', NULL, 20000.00, 'VND', true, now(), now()
            WHERE NOT EXISTS (SELECT 1 FROM gift_catalog WHERE name = 'Star');

            INSERT INTO gift_catalog (id, name, description, icon_url, price, currency, is_active, created_at, updated_at)
            SELECT gen_random_uuid(), 'Trophy', 'Recognize an excellent lesson.', NULL, 50000.00, 'VND', true, now(), now()
            WHERE NOT EXISTS (SELECT 1 FROM gift_catalog WHERE name = 'Trophy');

            INSERT INTO gift_catalog (id, name, description, icon_url, price, currency, is_active, created_at, updated_at)
            SELECT gen_random_uuid(), 'Crown', 'A special gift for an outstanding mentor.', NULL, 100000.00, 'VND', true, now(), now()
            WHERE NOT EXISTS (SELECT 1 FROM gift_catalog WHERE name = 'Crown');
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("DELETE FROM gift_catalog WHERE name IN ('Flower', 'Star', 'Trophy', 'Crown');");
    }
}
