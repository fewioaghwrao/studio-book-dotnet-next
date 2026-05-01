using Studiobook_backend.Data;
using Studiobook_backend.Entities;

namespace Studiobook_backend.Services
{
    public class AuditLogService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<AuditLogService> _logger;

        public AuditLogService(
            AppDbContext context,
            ILogger<AuditLogService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task WriteAsync(
            int? actorId,
            string action,
            string entity,
            int? entityId,
            string note)
        {
            try
            {
                var log = new AuditLog
                {
                    Ts = DateTime.UtcNow,
                    ActorId = actorId,
                    Action = action,
                    Entity = entity,
                    EntityId = entityId,
                    Note = note
                };

                _context.AuditLogs.Add(log);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                // 監査ログ保存失敗で本処理を止めない
                _logger.LogWarning(ex, "Audit log write failed. Action={Action}, Entity={Entity}, EntityId={EntityId}", action, entity, entityId);
            }
        }
    }
}
