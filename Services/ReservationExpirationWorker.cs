using EasyCars.Data;
using EasyCars.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace EasyCars.Services
{
    /// <summary>
    /// Background Worker – OnTemporel event: runs every hour to:
    ///  1. Detect finished reservations (DateFin passed) → mark as Terminee
    ///  2. Free the car back to Disponible
    /// </summary>
    public class ReservationExpirationWorker : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<ReservationExpirationWorker> _logger;
        private readonly TimeSpan _interval = TimeSpan.FromHours(1);

        public ReservationExpirationWorker(IServiceScopeFactory scopeFactory,
                                           ILogger<ReservationExpirationWorker> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("ReservationExpirationWorker started.");

            while (!stoppingToken.IsCancellationRequested)
            {
                await ProcessExpiredReservationsAsync();
                await Task.Delay(_interval, stoppingToken);
            }
        }

        private async Task ProcessExpiredReservationsAsync()
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var now = DateTime.UtcNow;

            // Find confirmed reservations that have passed their end date
            var expired = await context.Reservations
                .Include(r => r.Voiture)
                .Where(r => r.Statut == StatutReservation.Confirmee && r.DateFin < now)
                .ToListAsync();

            if (expired.Any())
            {
                foreach (var reservation in expired)
                {
                    reservation.Statut = StatutReservation.Terminee;

                    // Free up the car if it was marked as Louee
                    if (reservation.Voiture != null &&
                        reservation.Voiture.Statut == StatutVoiture.Louee)
                    {
                        reservation.Voiture.Statut = StatutVoiture.Disponible;
                    }
                }

                await context.SaveChangesAsync();
                _logger.LogInformation(
                    "[OnTemporel] {Count} reservation(s) marked Terminee and cars freed.",
                    expired.Count);
            }
        }
    }
}
