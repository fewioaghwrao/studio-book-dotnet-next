using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Studiobook_backend.Application.Interfaces;
using Studiobook_backend.Domain.Entities;

namespace Studiobook_backend.Infrastructure.Persistence
{
    public class AppDbContext : DbContext, IAppDbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users => Set<User>();
        public DbSet<Role> Roles => Set<Role>();
        public DbSet<UserRole> UserRoles => Set<UserRole>();
        public DbSet<VerificationToken> VerificationTokens => Set<VerificationToken>();

        public DbSet<PasswordResetToken> PasswordResetTokens => Set<PasswordResetToken>();

        public DbSet<Room> Rooms => Set<Room>();

        public DbSet<Closure> Closures => Set<Closure>();

        public DbSet<BusinessHour> BusinessHours => Set<BusinessHour>();

        public DbSet<PriceRule> PriceRules => Set<PriceRule>();

        public DbSet<Reservation> Reservations => Set<Reservation>();

        public DbSet<Review> Reviews => Set<Review>();

        public DbSet<ReservationChargeItem> ReservationChargeItems => Set<ReservationChargeItem>();
        public DbSet<AppSetting> AppSettings => Set<AppSetting>();
        public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

        public DbSet<AiSearchLog> AiSearchLogs => Set<AiSearchLog>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<User>(entity =>
            {
                entity.ToTable("Users");
                entity.HasKey(x => x.Id);

                entity.Property(x => x.Name)
                    .HasMaxLength(100)
                    .IsRequired();

                entity.Property(x => x.Kana)
                    .HasMaxLength(100)
                    .IsRequired();

                entity.Property(x => x.Email)
                    .HasMaxLength(255)
                    .IsRequired();

                entity.Property(x => x.PasswordHash)
                    .HasMaxLength(500)
                    .IsRequired();

                entity.Property(x => x.PostalCode)
                    .HasMaxLength(20)
                    .IsRequired();

                entity.Property(x => x.Address)
                    .HasMaxLength(300)
                    .IsRequired();

                entity.Property(x => x.PhoneNumber)
                    .HasMaxLength(30)
                    .IsRequired();

                entity.Property(x => x.UsageType)
                    .HasMaxLength(50)
                    .IsRequired();

                entity.Property(x => x.Enabled)
                    .IsRequired();

                entity.Property(x => x.EmailVerifiedAtUtc);

                entity.HasIndex(x => x.Email)
                    .IsUnique();
            });

            modelBuilder.Entity<Role>(entity =>
            {
                entity.ToTable("Roles");
                entity.HasKey(x => x.Id);

                entity.Property(x => x.Name)
                    .HasMaxLength(50)
                    .IsRequired();

                entity.HasIndex(x => x.Name)
                    .IsUnique();
            });

            modelBuilder.Entity<UserRole>(entity =>
            {
                entity.ToTable("UserRoles");
                entity.HasKey(x => x.Id);

                entity.HasIndex(x => new { x.UserId, x.RoleId })
                    .IsUnique();

                entity.HasOne(x => x.User)
                    .WithMany(x => x.UserRoles)
                    .HasForeignKey(x => x.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(x => x.Role)
                    .WithMany(x => x.UserRoles)
                    .HasForeignKey(x => x.RoleId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<VerificationToken>(entity =>
            {
                entity.ToTable("VerificationTokens");
                entity.HasKey(x => x.Id);

                entity.Property(x => x.Token)
                    .HasMaxLength(200)
                    .IsRequired();

                entity.Property(x => x.ExpiresAtUtc)
                    .IsRequired();

                entity.Property(x => x.CreatedAtUtc)
                    .IsRequired();

                entity.HasIndex(x => x.Token)
                    .IsUnique();

                entity.HasOne(x => x.User)
                    .WithMany(x => x.VerificationTokens)
                    .HasForeignKey(x => x.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<PasswordResetToken>(entity =>
            {
                entity.ToTable("PasswordResetTokens");
                entity.HasKey(x => x.Id);

                entity.Property(x => x.Token)
                    .HasMaxLength(200)
                    .IsRequired();

                entity.Property(x => x.ExpiresAtUtc)
                    .IsRequired();

                entity.Property(x => x.CreatedAtUtc)
                    .IsRequired();

                entity.HasIndex(x => x.Token)
                    .IsUnique();

                entity.HasOne(x => x.User)
                    .WithMany(x => x.PasswordResetTokens)
                    .HasForeignKey(x => x.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<Room>(entity =>
            {
                entity.ToTable("Rooms");
                entity.HasKey(x => x.Id);

                entity.Property(x => x.Name)
                    .HasMaxLength(200)
                    .IsRequired();

                entity.Property(x => x.ImageName)
                    .HasMaxLength(255)
                    .IsRequired(false);

                entity.Property(x => x.Description)
                    .HasMaxLength(2000)
                    .IsRequired(false);

                entity.Property(x => x.Price)
                    .IsRequired();

                entity.Property(x => x.Capacity)
                    .IsRequired();

                entity.Property(x => x.PostalCode)
                    .HasMaxLength(20)
                    .IsRequired();

                entity.Property(x => x.Address)
                    .HasMaxLength(300)
                    .IsRequired();

                entity.Property(x => x.CreatedAtUtc)
                    .IsRequired();

                entity.Property(x => x.UpdatedAtUtc)
                    .IsRequired();

                entity.HasIndex(x => new { x.Name, x.Address })
                    .IsUnique();

                entity.HasOne(x => x.User)
                    .WithMany(x => x.Rooms)
                    .HasForeignKey(x => x.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<Closure>(entity =>
            {
                entity.HasKey(x => x.Id);

                entity.Property(x => x.Reason)
                      .HasMaxLength(255);

                entity.HasOne(x => x.Room)
                      .WithMany(x => x.Closures)
                      .HasForeignKey(x => x.RoomId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<BusinessHour>(entity =>
            {
                entity.ToTable("BusinessHours");

                entity.HasKey(x => x.Id);

                entity.Property(x => x.DayOfWeek)
                      .IsRequired();

                entity.Property(x => x.StartTime)
                      .HasColumnType("time")
                      .IsRequired(false);

                entity.Property(x => x.EndTime)
                      .HasColumnType("time")
                      .IsRequired(false);

                entity.Property(x => x.IsHoliday)
                      .IsRequired();

                entity.Property(x => x.CreatedAtUtc)
                      .IsRequired();

                entity.Property(x => x.UpdatedAtUtc)
                      .IsRequired();

                entity.HasIndex(x => new { x.RoomId, x.DayOfWeek })
                      .IsUnique();

                entity.HasOne(x => x.Room)
                      .WithMany(x => x.BusinessHours)
                      .HasForeignKey(x => x.RoomId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<PriceRule>(entity =>
            {
                entity.ToTable("PriceRules");

                entity.HasKey(x => x.Id);

                entity.Property(x => x.RuleType)
                      .HasMaxLength(50)
                      .IsRequired();

                entity.Property(x => x.StartHour)
                      .HasColumnType("time")
                      .IsRequired(false);

                entity.Property(x => x.EndHour)
                      .HasColumnType("time")
                      .IsRequired(false);

                entity.Property(x => x.Multiplier)
                      .HasColumnType("decimal(5,2)")
                      .IsRequired(false);

                entity.Property(x => x.FlatFee)
                      .IsRequired(false);

                entity.Property(x => x.Note)
                      .HasMaxLength(500)
                      .IsRequired(false);

                entity.Property(x => x.CreatedAtUtc)
                      .IsRequired();

                entity.Property(x => x.UpdatedAtUtc)
                      .IsRequired();

                entity.HasOne(x => x.Room)
                      .WithMany(x => x.PriceRules)
                      .HasForeignKey(x => x.RoomId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(x => new { x.RoomId, x.RuleType, x.Weekday });
            });

            modelBuilder.Entity<Reservation>(entity =>
            {
                entity.ToTable("Reservations");

                entity.HasKey(x => x.Id);

                entity.Property(x => x.StartAt)
                      .IsRequired();

                entity.Property(x => x.EndAt)
                      .IsRequired();

                entity.Property(x => x.Amount)
                      .IsRequired();

                entity.Property(x => x.Status)
                      .HasMaxLength(50)
                      .IsRequired();

                entity.Property(x => x.PaymentIntentId)
                      .HasMaxLength(255)
                      .IsRequired(false);

                entity.Property(x => x.CreatedAtUtc)
                      .IsRequired();

                entity.Property(x => x.UpdatedAtUtc)
                      .IsRequired();

                entity.HasOne(x => x.Room)
                      .WithMany(x => x.Reservations)
                      .HasForeignKey(x => x.RoomId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(x => x.User)
                      .WithMany(x => x.Reservations)
                      .HasForeignKey(x => x.UserId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(x => x.RoomId);
                entity.HasIndex(x => x.UserId);
                entity.HasIndex(x => x.Status);
                entity.HasIndex(x => x.StartAt);

                // Webhook の再送・同時受信でも同じ決済から予約を二重に作らない（NULL は重複可）
                entity.HasIndex(x => x.PaymentIntentId)
                      .IsUnique();
            });

            modelBuilder.Entity<Review>(entity =>
            {
                entity.ToTable("Reviews");

                entity.HasKey(x => x.Id);

                entity.Property(x => x.Score)
                      .IsRequired();

                entity.Property(x => x.Content)
                      .HasMaxLength(2000)
                      .IsRequired();

                entity.Property(x => x.PublicVisible)
                      .IsRequired();

                entity.Property(x => x.HiddenReason)
                      .HasMaxLength(500)
                      .IsRequired(false);

                entity.Property(x => x.HostReply)
                      .HasMaxLength(2000)
                      .IsRequired(false);

                entity.Property(x => x.HostReplyAt)
                      .IsRequired(false);

                entity.Property(x => x.CreatedAtUtc)
                      .IsRequired();

                entity.Property(x => x.UpdatedAtUtc)
                      .IsRequired();

                entity.HasOne(x => x.Room)
                      .WithMany(x => x.Reviews)
                      .HasForeignKey(x => x.RoomId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(x => x.User)
                      .WithMany(x => x.Reviews)
                      .HasForeignKey(x => x.UserId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(x => x.RoomId);
                entity.HasIndex(x => x.UserId);
                entity.HasIndex(x => x.Score);
                entity.HasIndex(x => x.PublicVisible);
                entity.HasIndex(x => x.CreatedAtUtc);

                // 同一ユーザーは同一スタジオへ 1 件だけレビューできる（同時投稿でも重複させない）
                entity.HasIndex(x => new { x.RoomId, x.UserId })
                      .IsUnique();
            });

            modelBuilder.Entity<ReservationChargeItem>(entity =>
            {
                entity.ToTable("ReservationChargeItems");

                entity.HasKey(x => x.Id);

                entity.Property(x => x.Kind)
                      .HasMaxLength(50)
                      .IsRequired();

                entity.Property(x => x.Description)
                      .HasMaxLength(500)
                      .IsRequired(false);

                entity.Property(x => x.SliceAmount)
                      .IsRequired();

                entity.Property(x => x.SliceStart)
                      .IsRequired(false);

                entity.Property(x => x.SliceEnd)
                      .IsRequired(false);

                entity.Property(x => x.UnitRatePerHour)
                      .IsRequired(false);

                entity.HasOne(x => x.Reservation)
                      .WithMany(x => x.ChargeItems)
                      .HasForeignKey(x => x.ReservationId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(x => x.ReservationId);
                entity.HasIndex(x => x.SliceStart);
            });

            modelBuilder.Entity<AppSetting>(entity =>
            {
                entity.ToTable("AppSettings");
                entity.HasKey(x => x.Id);

                entity.Property(x => x.Key)
                    .HasMaxLength(100)
                    .IsRequired();

                entity.Property(x => x.Value)
                    .HasMaxLength(100)
                    .IsRequired();

                entity.Property(x => x.UpdatedAtUtc)
                    .IsRequired();

                entity.HasIndex(x => x.Key)
                    .IsUnique();
            });

            modelBuilder.Entity<AuditLog>(entity =>
            {
                entity.ToTable("AuditLogs");
                entity.HasKey(x => x.Id);

                entity.Property(x => x.Ts)
                    .IsRequired();

                entity.Property(x => x.Action)
                    .HasMaxLength(100)
                    .IsRequired();

                entity.Property(x => x.Entity)
                    .HasMaxLength(100)
                    .IsRequired();

                entity.Property(x => x.Note)
                    .HasMaxLength(2000)
                    .IsRequired(false);

                entity.HasIndex(x => x.Ts);
                entity.HasIndex(x => x.ActorId);
                entity.HasIndex(x => x.Action);
                entity.HasIndex(x => x.Entity);
                entity.HasIndex(x => x.EntityId);
            });

            modelBuilder.Entity<AiSearchLog>(entity =>
            {
                entity.ToTable("AiSearchLogs");
                entity.HasKey(x => x.Id);

                entity.Property(x => x.CreatedAtUtc)
                    .IsRequired();

                entity.Property(x => x.Query)
                    .HasMaxLength(500)
                    .IsRequired();

                entity.Property(x => x.IpAddress)
                    .HasMaxLength(100)
                    .IsRequired(false);

                entity.Property(x => x.UserId)
                    .IsRequired(false);

                entity.Property(x => x.Model)
                    .HasMaxLength(100)
                    .IsRequired(false);

                entity.Property(x => x.Succeeded)
                    .IsRequired();

                entity.Property(x => x.ResultCount)
                    .IsRequired();

                entity.Property(x => x.ErrorMessage)
                    .HasMaxLength(1000)
                    .IsRequired(false);

                entity.HasIndex(x => x.CreatedAtUtc);
                entity.HasIndex(x => x.IpAddress);
                entity.HasIndex(x => x.UserId);
                entity.HasIndex(x => x.Succeeded);
            });

            ApplyUtcDateTimeKind(modelBuilder);
        }

        /// <summary>
        /// UTC で保存している記録用日時の列名（*Utc 以外）。
        /// 予約・休業日の StartAt / EndAt などは日本時間の壁時計時刻のため対象外。
        /// </summary>
        private static readonly HashSet<string> UtcDateTimePropertyNames = new(StringComparer.Ordinal)
        {
            nameof(AuditLog.Ts),
            nameof(Review.HostReplyAt),
            $"{nameof(Closure)}.{nameof(Closure.CreatedAt)}",
        };

        // DB（MySQL の datetime）から読んだ値は Kind = Unspecified になり、JSON に "Z" が付かない。
        // ブラウザが日本時間として解釈して 9 時間ずれるため、読み込み時に UTC であることを明示する。
        private static readonly ValueConverter<DateTime, DateTime> UtcKindConverter = new(
            v => v,
            v => DateTime.SpecifyKind(v, DateTimeKind.Utc));

        private static void ApplyUtcDateTimeKind(ModelBuilder modelBuilder)
        {
            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                foreach (var property in entityType.GetProperties())
                {
                    if (property.ClrType != typeof(DateTime) && property.ClrType != typeof(DateTime?))
                    {
                        continue;
                    }

                    if (IsUtcDateTimeColumn(entityType.ClrType.Name, property.Name))
                    {
                        property.SetValueConverter(UtcKindConverter);
                    }
                }
            }
        }

        private static bool IsUtcDateTimeColumn(string entityName, string propertyName)
        {
            return propertyName.EndsWith("Utc", StringComparison.Ordinal) ||
                   UtcDateTimePropertyNames.Contains(propertyName) ||
                   UtcDateTimePropertyNames.Contains($"{entityName}.{propertyName}");
        }
    }
}