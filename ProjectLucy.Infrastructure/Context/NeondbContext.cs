using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using ProjectLucy.Infrastructure;

namespace ProjectLucy.Infrastructure.Context;

public partial class NeondbContext : DbContext
{
    public NeondbContext()
    {
    }

    public NeondbContext(DbContextOptions<NeondbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<RefreshToken> RefreshTokens { get; set; }

    public virtual DbSet<Role> Roles { get; set; }

    public virtual DbSet<RolePrice> RolePrices { get; set; }

    public virtual DbSet<Transaction> Transactions { get; set; }

    public virtual DbSet<TransactionType> TransactionTypes { get; set; }

    public virtual DbSet<User> Users { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        => optionsBuilder.UseNpgsql("Name=ConnectionStrings:DefaultConnection");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.HasKey(e => e.TokenId).HasName("refresh_token_pkey");

            entity.ToTable("refresh_token");

            entity.HasIndex(e => e.ExpiredAt, "idx_refresh_token_expired");

            entity.HasIndex(e => e.IsRevoked, "idx_refresh_token_revoked");

            entity.HasIndex(e => e.UserId, "idx_refresh_token_user_id");

            entity.HasIndex(e => e.Token, "refresh_token_token_key").IsUnique();

            entity.Property(e => e.TokenId)
                .ValueGeneratedNever()
                .HasColumnName("token_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("created_at");
            entity.Property(e => e.ExpiredAt).HasColumnName("expired_at");
            entity.Property(e => e.IsRevoked)
                .HasDefaultValue(false)
                .HasColumnName("is_revoked");
            entity.Property(e => e.Token).HasColumnName("token");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.User).WithMany(p => p.RefreshTokens)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("fk_refresh_token_user");
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(e => e.RoleId).HasName("role_pkey");

            entity.ToTable("role");

            entity.HasIndex(e => e.RoleCode, "role_role_code_key").IsUnique();

            entity.Property(e => e.RoleId).HasColumnName("role_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("created_at");
            entity.Property(e => e.RoleCode)
                .HasMaxLength(50)
                .HasColumnName("role_code");
            entity.Property(e => e.RoleName)
                .HasMaxLength(100)
                .HasColumnName("role_name");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
        });

        modelBuilder.Entity<RolePrice>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("role_price_pkey");

            entity.ToTable("role_price");

            entity.HasIndex(e => e.IsActive, "idx_role_price_active");

            entity.HasIndex(e => e.RoleId, "idx_role_price_role_id");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.Currency)
                .HasMaxLength(10)
                .HasDefaultValueSql("'VND'::character varying")
                .HasColumnName("currency");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.Duration).HasColumnName("duration");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("is_active");
            entity.Property(e => e.Price)
                .HasPrecision(12, 2)
                .HasColumnName("price");
            entity.Property(e => e.RoleId).HasColumnName("role_id");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("updated_at");

            entity.HasOne(d => d.Role).WithMany(p => p.RolePrices)
                .HasForeignKey(d => d.RoleId)
                .HasConstraintName("role_price_role_id_fkey");
        });

        modelBuilder.Entity<Transaction>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("transactions_pkey");

            entity.ToTable("transactions");

            entity.HasIndex(e => e.CreatedAt, "idx_transactions_created_at");

            entity.HasIndex(e => e.ReferenceCode, "idx_transactions_reference_code");

            entity.HasIndex(e => e.Status, "idx_transactions_status");

            entity.HasIndex(e => e.TransactionTypeId, "idx_transactions_type_id");

            entity.HasIndex(e => e.UserId, "idx_transactions_user_id");

            entity.HasIndex(e => e.ReferenceCode, "transactions_reference_code_key").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Amount)
                .HasPrecision(15, 2)
                .HasColumnName("amount");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.Currency)
                .HasMaxLength(10)
                .HasDefaultValueSql("'VND'::character varying")
                .HasColumnName("currency");
            entity.Property(e => e.ReferenceCode)
                .HasMaxLength(100)
                .HasColumnName("reference_code");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValueSql("'pending'::character varying")
                .HasColumnName("status");
            entity.Property(e => e.TransactionContent).HasColumnName("transaction_content");
            entity.Property(e => e.TransactionTypeId).HasColumnName("transaction_type_id");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("updated_at");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.TransactionType).WithMany(p => p.Transactions)
                .HasForeignKey(d => d.TransactionTypeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("transactions_transaction_type_id_fkey");
        });

        modelBuilder.Entity<TransactionType>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("transaction_type_pkey");

            entity.ToTable("transaction_type");

            entity.HasIndex(e => e.Code, "idx_transaction_type_code");

            entity.HasIndex(e => e.Code, "transaction_type_code_key").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Code)
                .HasMaxLength(50)
                .HasColumnName("code");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("is_active");
            entity.Property(e => e.Name)
                .HasMaxLength(100)
                .HasColumnName("name");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("updated_at");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.UserId).HasName("user_pkey");

            entity.ToTable("user");

            entity.HasIndex(e => e.UserEmail, "idx_user_email");

            entity.HasIndex(e => e.UserPhoneNumber, "idx_user_phone");

            entity.HasIndex(e => e.UserEmail, "user_user_email_key").IsUnique();

            entity.HasIndex(e => e.UserPhoneNumber, "user_user_phone_number_key").IsUnique();

            entity.Property(e => e.UserId)
                .ValueGeneratedNever()
                .HasColumnName("user_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("created_at");
            entity.Property(e => e.RoleId).HasColumnName("role_id");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
            entity.Property(e => e.UserBirthday).HasColumnName("user_birthday");
            entity.Property(e => e.UserEmail)
                .HasMaxLength(255)
                .HasColumnName("user_email");
            entity.Property(e => e.UserFullName)
                .HasMaxLength(255)
                .HasColumnName("user_full_name");
            entity.Property(e => e.UserHashPassword)
                .HasMaxLength(255)
                .HasColumnName("user_hash_password");
            entity.Property(e => e.UserPhoneNumber)
                .HasMaxLength(30)
                .HasColumnName("user_phone_number");

            entity.HasOne(d => d.Role).WithMany(p => p.Users)
                .HasForeignKey(d => d.RoleId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_user_role");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
