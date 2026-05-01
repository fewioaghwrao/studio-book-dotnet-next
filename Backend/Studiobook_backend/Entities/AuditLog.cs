namespace Studiobook_backend.Entities
{
    public class AuditLog
    {
        public int Id { get; set; }

        public DateTime Ts { get; set; }

        public int? ActorId { get; set; }

        public string Action { get; set; } = string.Empty;

        public string Entity { get; set; } = string.Empty;

        public int? EntityId { get; set; }

        public string Note { get; set; } = string.Empty;
    }
}