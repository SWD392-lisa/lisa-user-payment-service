using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectLucy.Infrastructure.Persistence.Migrations;

/// <summary>
/// Persists the anonymous identities used by live-room learners and the room
/// gift routing/outbox data. The persona feature was introduced after the
/// previous migration, so existing databases were missing these tables.
/// </summary>
[DbContext(typeof(NeonDbContext))]
[Migration("20260723030000_AddRoomPrivacyAndGiftSchema")]
public partial class AddRoomPrivacyAndGiftSchema : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            CREATE EXTENSION IF NOT EXISTS pgcrypto;

            CREATE TABLE IF NOT EXISTS "anonymous_room_identity" (
                "id" uuid PRIMARY KEY DEFAULT gen_random_uuid(),
                "user_id" uuid NOT NULL,
                "room_session_id" uuid NOT NULL,
                "anonymous_id" uuid NOT NULL,
                "display_name" varchar(80) NOT NULL,
                "persona_code" varchar(40) NOT NULL,
                "persona_asset_url" varchar(255) NOT NULL,
                "created_at" timestamptz NOT NULL DEFAULT now(),
                "expires_at" timestamptz NOT NULL,
                CONSTRAINT "fk_anonymous_room_identity_user"
                    FOREIGN KEY ("user_id") REFERENCES "user" ("user_id") ON DELETE CASCADE
            );

            CREATE TABLE IF NOT EXISTS "room_gift_recipient" (
                "id" uuid PRIMARY KEY DEFAULT gen_random_uuid(),
                "room_session_id" uuid NOT NULL,
                "recipient_user_id" uuid NOT NULL,
                "is_active" boolean NOT NULL DEFAULT true,
                "created_at" timestamptz NOT NULL DEFAULT now(),
                "expires_at" timestamptz NOT NULL,
                CONSTRAINT "fk_room_gift_recipient_user"
                    FOREIGN KEY ("recipient_user_id") REFERENCES "user" ("user_id") ON DELETE CASCADE
            );

            CREATE TABLE IF NOT EXISTS "gift_event_outbox" (
                "id" uuid PRIMARY KEY DEFAULT gen_random_uuid(),
                "gift_transaction_id" uuid NOT NULL,
                "payload" jsonb NOT NULL,
                "status" varchar(20) NOT NULL DEFAULT 'PENDING',
                "attempts" integer NOT NULL DEFAULT 0,
                "next_attempt_at" timestamptz NOT NULL,
                "created_at" timestamptz NOT NULL DEFAULT now(),
                "sent_at" timestamptz,
                "last_error" text
            );

            CREATE UNIQUE INDEX IF NOT EXISTS "uq_anonymous_room_identity_user_session"
                ON "anonymous_room_identity" ("user_id", "room_session_id");
            CREATE UNIQUE INDEX IF NOT EXISTS "uq_anonymous_room_identity_alias"
                ON "anonymous_room_identity" ("anonymous_id");
            CREATE UNIQUE INDEX IF NOT EXISTS "uq_room_gift_recipient_session"
                ON "room_gift_recipient" ("room_session_id");
            CREATE UNIQUE INDEX IF NOT EXISTS "uq_gift_event_outbox_transaction"
                ON "gift_event_outbox" ("gift_transaction_id");
            CREATE INDEX IF NOT EXISTS "idx_gift_event_outbox_pending"
                ON "gift_event_outbox" ("status", "next_attempt_at");
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "gift_event_outbox");
        migrationBuilder.DropTable(name: "room_gift_recipient");
        migrationBuilder.DropTable(name: "anonymous_room_identity");
    }
}
