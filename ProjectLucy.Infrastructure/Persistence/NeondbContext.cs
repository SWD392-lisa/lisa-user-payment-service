using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using ProjectLucy.Domain.Entities;
using ProjectLucy.Infrastructure;

namespace ProjectLucy.Infrastructure.Persistence;

public partial class NeonDbContext : DbContext
{
    public NeonDbContext()
    {
    }

    public NeonDbContext(DbContextOptions<NeonDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<GiftCatalog> GiftCatalogs { get; set; }

    public virtual DbSet<GiftTransaction> GiftTransactions { get; set; }

    public virtual DbSet<PaymentMethod> PaymentMethods { get; set; }

    public virtual DbSet<RefreshToken> RefreshTokens { get; set; }

    public virtual DbSet<Role> Roles { get; set; }

    public virtual DbSet<RolePrice> RolePrices { get; set; }

    public virtual DbSet<RoleUpgradeOrder> RoleUpgradeOrders { get; set; }

    public virtual DbSet<Transaction> Transactions { get; set; }

    public virtual DbSet<TransactionType> TransactionTypes { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<Wallet> Wallets { get; set; }

    public virtual DbSet<WalletLedger> WalletLedgers { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        => optionsBuilder.UseNpgsql("Name=ConnectionStrings:DefaultConnection");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<GiftCatalog>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("gift_catalog_pkey");

            entity.ToTable("gift_catalog", tb => tb.HasComment("Danh mục quà ảo có thể tặng trong phòng học. is_active = FALSE để ẩn khỏi UI mà không xóa lịch sử."));

            entity.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            entity.Property(e => e.Currency).HasDefaultValueSql("'VND'::character varying");
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("now()");
        });

        modelBuilder.Entity<GiftTransaction>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("gift_transaction_pkey");

            entity.ToTable("gift_transaction", tb => tb.HasComment("Chi tiết tặng quà. transaction_id trỏ đến bản ghi DEBIT trong transactions. Luồng: sender ví DEBIT → wallet_ledger → gift_transaction. Receiver nhận CREDIT riêng qua transaction mới."));

            entity.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            entity.Property(e => e.Quantity).HasDefaultValue(1);

            entity.HasOne(d => d.Gift).WithMany(p => p.GiftTransactions)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_gift_txn_gift");

            entity.HasOne(d => d.Receiver).WithMany(p => p.GiftTransactionReceivers)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_gift_txn_receiver");

            entity.HasOne(d => d.Sender).WithMany(p => p.GiftTransactionSenders)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_gift_txn_sender");

            entity.HasOne(d => d.Transaction).WithOne(p => p.GiftTransaction)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_gift_txn_transaction");
        });

        modelBuilder.Entity<PaymentMethod>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("payment_method_pkey");

            entity.ToTable("payment_method", tb => tb.HasComment("Lưu chi tiết từ cổng thanh toán (VNPAY/Momo/ZaloPay...). metadata giữ nguyên payload gốc để debug & đối soát."));

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("now()");

            entity.HasOne(d => d.Transaction).WithOne(p => p.PaymentMethod).HasConstraintName("fk_payment_method_transaction");
        });

        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.HasKey(e => e.TokenId).HasName("refresh_token_pkey");

            entity.Property(e => e.TokenId).ValueGeneratedNever();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.IsRevoked).HasDefaultValue(false);

            entity.HasOne(d => d.User).WithMany(p => p.RefreshTokens).HasConstraintName("fk_refresh_token_user");
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(e => e.RoleId).HasName("role_pkey");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
        });

        modelBuilder.Entity<RolePrice>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("role_price_pkey");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            entity.Property(e => e.Currency).HasDefaultValueSql("'VND'::character varying");
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("now()");

            entity.HasOne(d => d.Role).WithMany(p => p.RolePrices).HasConstraintName("role_price_role_id_fkey");
        });

        modelBuilder.Entity<RoleUpgradeOrder>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("role_upgrade_order_pkey");

            entity.ToTable("role_upgrade_order", tb => tb.HasComment("Đơn hàng nâng cấp tài khoản (Pro/Super). activated_at được set khi transaction → completed. expires_at = activated_at + role_price.duration (NULL = không hết hạn)."));

            entity.HasIndex(e => new { e.UserId, e.ExpiresAt }, "idx_upgrade_active").HasFilter("((activated_at IS NOT NULL) AND (cancelled_at IS NULL))");

            entity.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("now()");

            entity.HasOne(d => d.FromRole).WithMany(p => p.RoleUpgradeOrderFromRoles)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_upgrade_from_role");

            entity.HasOne(d => d.RolePrice).WithMany(p => p.RoleUpgradeOrders)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_upgrade_role_price");

            entity.HasOne(d => d.ToRole).WithMany(p => p.RoleUpgradeOrderToRoles)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_upgrade_to_role");

            entity.HasOne(d => d.Transaction).WithOne(p => p.RoleUpgradeOrder)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_upgrade_transaction");

            entity.HasOne(d => d.User).WithMany(p => p.RoleUpgradeOrders)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_upgrade_user");
        });

        modelBuilder.Entity<Transaction>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("transactions_pkey");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            entity.Property(e => e.Currency).HasDefaultValueSql("'VND'::character varying");
            entity.Property(e => e.Status).HasDefaultValueSql("'pending'::character varying");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("now()");

            entity.HasOne(d => d.TransactionType).WithMany(p => p.Transactions)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("transactions_transaction_type_id_fkey");

            entity.HasOne(d => d.User).WithMany(p => p.Transactions)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_transactions_user");
        });

        modelBuilder.Entity<TransactionType>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("transaction_type_pkey");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("now()");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.UserId).HasName("user_pkey");

            entity.Property(e => e.UserId).ValueGeneratedNever();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(d => d.Role).WithMany(p => p.Users)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_user_role");
        });

        modelBuilder.Entity<Wallet>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("wallet_pkey");

            entity.ToTable("wallet", tb => tb.HasComment("Ví điện tử 1-1 với user. Balance luôn >= 0, mọi thay đổi phải qua wallet_ledger."));

            entity.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
            entity.Property(e => e.Balance).HasDefaultValueSql("0.00");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            entity.Property(e => e.Currency).HasDefaultValueSql("'VND'::character varying");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("now()");

            entity.HasOne(d => d.User).WithOne(p => p.Wallet).HasConstraintName("fk_wallet_user");
        });

        modelBuilder.Entity<WalletLedger>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("wallet_ledger_pkey");

            entity.ToTable("wallet_ledger", tb => tb.HasComment("Sổ cái bất biến. Không UPDATE/DELETE — chỉ INSERT. Dùng để audit, đối soát, và tái tính balance nếu cần."));

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");

            entity.HasOne(d => d.Transaction).WithMany(p => p.WalletLedgers)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_ledger_transaction");

            entity.HasOne(d => d.Wallet).WithMany(p => p.WalletLedgers)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_ledger_wallet");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
