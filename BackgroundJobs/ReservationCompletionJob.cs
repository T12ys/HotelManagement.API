using HotelWebApplication.Data;
using HotelWebApplication.Enums;
using HotelWebApplication.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HotelWebApplication.BackgroundJobs;

/// <summary>
/// Фоновая задача: каждый час переводит подтверждённые брони
/// у которых EndDate прошла → Completed.
/// </summary>
public class ReservationCompletionJob : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ReservationCompletionJob> _logger;

    // Интервал проверки — каждый час
    private static readonly TimeSpan Interval = TimeSpan.FromHours(1);

    public ReservationCompletionJob(
        IServiceScopeFactory scopeFactory,
        ILogger<ReservationCompletionJob> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("ReservationCompletionJob started.");

        // Запускаем сразу при старте приложения
        await RunAsync(stoppingToken);

        using var timer = new PeriodicTimer(Interval);

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await RunAsync(stoppingToken);
        }
    }

    private async Task RunAsync(CancellationToken ct)
    {
        try
        {
            // BackgroundService живёт дольше чем Scoped-сервисы,
            // поэтому создаём scope вручную
            await using var scope = _scopeFactory.CreateAsyncScope();

            var db = scope.ServiceProvider.GetRequiredService<HotelDbContext>();
            var audit = scope.ServiceProvider.GetRequiredService<IAuditLogService>();

            var now = DateTime.UtcNow.Date;

            var toComplete = await db.Reservations
                .Where(r => r.Status == ReservationStatus.Confirmed &&
                            r.EndDate <= now)
                .ToListAsync(ct);

            if (toComplete.Count == 0)
                return;

            foreach (var reservation in toComplete)
            {
                reservation.Status = ReservationStatus.Completed;
                reservation.UpdatedAt = DateTime.UtcNow;
            }

            await db.SaveChangesAsync(ct);

            // Пишем в AuditLog одной записью на каждую бронь
            foreach (var reservation in toComplete)
            {
                await audit.LogAsync(
                    actionType: "AutoCompleted",
                    entityType: "Reservation",
                    entityId: reservation.Id.ToString(),
                    oldValue: ReservationStatus.Confirmed.ToString(),
                    newValue: ReservationStatus.Completed.ToString());
            }

            _logger.LogInformation(
                "ReservationCompletionJob: {Count} reservation(s) marked as Completed.",
                toComplete.Count);
        }
        catch (OperationCanceledException)
        {
            // Приложение останавливается — выходим тихо
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ReservationCompletionJob failed.");
        }
    }
}