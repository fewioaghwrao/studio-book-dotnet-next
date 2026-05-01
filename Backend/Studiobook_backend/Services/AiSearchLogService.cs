using Microsoft.Extensions.Options;
using Studiobook_backend.Data;
using Studiobook_backend.Entities;
using Studiobook_backend.Settings;

namespace Studiobook_backend.Services;

public class AiSearchLogService
{
    private readonly AppDbContext _context;
    private readonly OpenAiSettings _settings;

    public AiSearchLogService(
        AppDbContext context,
        IOptions<OpenAiSettings> options)
    {
        _context = context;
        _settings = options.Value;
    }

    public async Task WriteAsync(
        string query,
        string? ipAddress,
        int? userId,
        bool succeeded,
        int resultCount,
        string? errorMessage = null)
    {
        var normalizedQuery = query.Trim();

        var log = new AiSearchLog
        {
            CreatedAtUtc = DateTime.UtcNow,
            Query = normalizedQuery.Length > 500
                ? normalizedQuery[..500]
                : normalizedQuery,
            IpAddress = string.IsNullOrWhiteSpace(ipAddress)
                ? null
                : ipAddress.Length > 100
                    ? ipAddress[..100]
                    : ipAddress,
            UserId = userId,
            Model = string.IsNullOrWhiteSpace(_settings.Model)
                ? null
                : _settings.Model.Length > 100
                    ? _settings.Model[..100]
                    : _settings.Model,
            Succeeded = succeeded,
            ResultCount = resultCount,
            ErrorMessage = string.IsNullOrWhiteSpace(errorMessage)
                ? null
                : errorMessage.Length > 1000
                    ? errorMessage[..1000]
                    : errorMessage
        };

        _context.AiSearchLogs.Add(log);
        await _context.SaveChangesAsync();
    }
}