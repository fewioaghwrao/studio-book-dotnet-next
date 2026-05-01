namespace Studiobook_backend.Dtos.Admin
{
    public class AdminAuditLogListResponseDto
    {
        public List<AdminAuditLogRowDto> Items { get; set; } = new();

        public int Page { get; set; }

        public int PageSize { get; set; }

        public int TotalCount { get; set; }

        public int TotalPages { get; set; }
    }

    public class AdminAuditLogRowDto
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