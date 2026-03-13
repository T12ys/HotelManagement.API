namespace HotelWebApplication.Services.Interfaces
{
    public interface IAuditLogService
    {
        Task LogAsync(
            string actionType,
            string entityType,
            string entityId,
            string? oldValue = null,
            string? newValue = null,
            Guid? actorUserId = null,
            string? ip = null);
    }

}
