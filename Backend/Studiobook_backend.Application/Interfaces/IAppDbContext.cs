using Microsoft.EntityFrameworkCore;
using Studiobook_backend.Domain.Entities;

namespace Studiobook_backend.Application.Interfaces;

/// <summary>
/// Application 層から永続化層へアクセスするための抽象。
/// 実装は Infrastructure 層の AppDbContext です。
/// </summary>
public interface IAppDbContext
{
    DbSet<User> Users { get; }
    DbSet<Role> Roles { get; }
    DbSet<UserRole> UserRoles { get; }
    DbSet<VerificationToken> VerificationTokens { get; }
    DbSet<PasswordResetToken> PasswordResetTokens { get; }
    DbSet<Room> Rooms { get; }
    DbSet<Closure> Closures { get; }
    DbSet<BusinessHour> BusinessHours { get; }
    DbSet<PriceRule> PriceRules { get; }
    DbSet<Reservation> Reservations { get; }
    DbSet<Review> Reviews { get; }
    DbSet<ReservationChargeItem> ReservationChargeItems { get; }
    DbSet<AppSetting> AppSettings { get; }
    DbSet<AuditLog> AuditLogs { get; }
    DbSet<AiSearchLog> AiSearchLogs { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
