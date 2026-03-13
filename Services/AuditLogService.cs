using HotelWebApplication.Data;
using HotelWebApplication.Models;
using HotelWebApplication.Services.Interfaces;

namespace HotelWebApplication.Services;

public class AuditLogService : IAuditLogService
{
    private readonly HotelDbContext _db;

    public AuditLogService(HotelDbContext db)
    {
        _db = db;
    }

    public async Task LogAsync(
        string actionType,
        string entityType,
        string entityId,
        string? oldValue = null,
        string? newValue = null,
        Guid? actorUserId = null,
        string? ip = null)
    {
        var entry = new AuditLog
        {
            ActionType = actionType,
            EntityType = entityType,
            EntityId = entityId,
            OldValue = oldValue,
            NewValue = newValue,
            ActorUserId = actorUserId,
            IP = ip,
            Timestamp = DateTime.UtcNow
        };

        _db.AuditLogs.Add(entry);
        await _db.SaveChangesAsync();
    }
}