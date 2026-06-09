using Microsoft.EntityFrameworkCore;
using ProjectLucy.Domain.Entities;
using PaymentEntity = ProjectLucy.Domain.Entities.Payment;

namespace ProjectLucy.Infrastructure.Persistence;

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
    public virtual DbSet<User> Users { get; set; }
    public virtual DbSet<PaymentEntity> Payments { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            optionsBuilder.UseNpgsql("Name=ConnectionStrings:DefaultConnection");
        }
    }

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

        ConfigurePayment(modelBuilder);

        OnModelCreatingPartial(modelBuilder);
    }

    private void ConfigurePayment(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PaymentEntity>(entity =>
        {
            entity.HasKey(e => e.PaymentId).HasName("payment_pkey");

            entity.ToTable("payment");

            entity.HasIndex(e => e.OrderInvoiceNumber, "idx_payment_invoice").IsUnique();
            entity.HasIndex(e => e.TransactionId, "idx_payment_transaction_id");

            entity.Property(e => e.PaymentId)
                .ValueGeneratedNever()
                .HasColumnName("payment_id");
            entity.Property(e => e.OrderInvoiceNumber)
                .IsRequired()
                .HasMaxLength(100)
                .HasColumnName("order_invoice_number");
            entity.Property(e => e.OrderAmount)
                .HasColumnName("order_amount");
            entity.Property(e => e.OrderDescription)
                .IsRequired()
                .HasMaxLength(255)
                .HasColumnName("order_description");
            entity.Property(e => e.CustomerId)
                .HasMaxLength(100)
                .HasColumnName("customer_id");
            entity.Property(e => e.Status)
                .IsRequired()
                .HasMaxLength(20)
                .HasDefaultValue("PENDING")
                .HasColumnName("status");
            entity.Property(e => e.TransactionId)
                .HasMaxLength(100)
                .HasColumnName("transaction_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
        });
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
