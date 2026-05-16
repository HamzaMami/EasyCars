using EasyCars.Data;
using EasyCars.Models;
using EasyCars.Models.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EasyCars.Controllers
{
    [Authorize(Roles = "AgenceAdmin,SuperAdmin")]
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DashboardController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var now = DateTime.Now;
            var firstOfYear = new DateTime(now.Year, 1, 1);

            // ── KPIs ──────────────────────────────────────────────
            var totalVoitures    = await _context.Voitures.CountAsync();
            var voituresLouees   = await _context.Voitures.CountAsync(v => v.Statut == StatutVoiture.Louee);
            var voituresDispo    = await _context.Voitures.CountAsync(v => v.Statut == StatutVoiture.Disponible);
            var totalReservations = await _context.Reservations.CountAsync();
            var revenuTotal      = await _context.Reservations
                                       .Where(r => r.Statut == StatutReservation.Confirmee || r.Statut == StatutReservation.Terminee)
                                       .SumAsync(r => (decimal?)r.MontantTotal) ?? 0;
            var tauxOccupation   = totalVoitures > 0 ? (double)voituresLouees / totalVoitures * 100 : 0;

            // ── Revenue per month (current year) ──────────────────
            var revenueByMonth = await _context.Reservations
                .Where(r => r.DateDebut >= firstOfYear &&
                            (r.Statut == StatutReservation.Confirmee || r.Statut == StatutReservation.Terminee))
                .GroupBy(r => r.DateDebut.Month)
                .Select(g => new { Month = g.Key, Total = g.Sum(r => r.MontantTotal) })
                .OrderBy(x => x.Month)
                .ToListAsync();

            var monthLabels  = Enumerable.Range(1, 12)
                               .Select(m => new DateTime(now.Year, m, 1).ToString("MMM"))
                               .ToArray();
            var revenueData  = Enumerable.Range(1, 12)
                               .Select(m => revenueByMonth.FirstOrDefault(x => x.Month == m)?.Total ?? 0)
                               .ToArray();

            // ── Revenue per agency ────────────────────────────────
            var revenueByAgence = await _context.Reservations
                .Include(r => r.Voiture).ThenInclude(v => v.Agence)
                .Where(r => r.Statut == StatutReservation.Confirmee || r.Statut == StatutReservation.Terminee)
                .GroupBy(r => r.Voiture.Agence.Nom)
                .Select(g => new { AgenceName = g.Key, Total = g.Sum(r => r.MontantTotal) })
                .ToListAsync();

            // ── Average rating per agency ─────────────────────────
            var avgRatingByAgence = await _context.Feedbacks
                .Where(f => f.AgenceId.HasValue)
                .Include(f => f.Agence)
                .GroupBy(f => f.Agence!.Nom)
                .Select(g => new { AgenceName = g.Key, AvgNote = g.Average(f => (double)f.Note) })
                .ToListAsync();

            // ── Recent reservations ───────────────────────────────
            var recentReservations = await _context.Reservations
                .Include(r => r.Utilisateur)
                .Include(r => r.Voiture).ThenInclude(v => v.Agence)
                .OrderByDescending(r => r.Id)
                .Take(8)
                .ToListAsync();

            ViewBag.TotalVoitures        = totalVoitures;
            ViewBag.VoitureDispo         = voituresDispo;
            ViewBag.VoitureLouees        = voituresLouees;
            ViewBag.TotalReservations    = totalReservations;
            ViewBag.RevenuTotal          = revenuTotal;
            ViewBag.TauxOccupation       = tauxOccupation;
            ViewBag.MonthLabels          = System.Text.Json.JsonSerializer.Serialize(monthLabels);
            ViewBag.RevenueData          = System.Text.Json.JsonSerializer.Serialize(revenueData);
            ViewBag.AgenceLabels         = System.Text.Json.JsonSerializer.Serialize(revenueByAgence.Select(x => x.AgenceName).ToArray());
            ViewBag.AgenceRevenue        = System.Text.Json.JsonSerializer.Serialize(revenueByAgence.Select(x => x.Total).ToArray());
            ViewBag.RatingAgenceLabels   = System.Text.Json.JsonSerializer.Serialize(avgRatingByAgence.Select(x => x.AgenceName).ToArray());
            ViewBag.RatingAgenceData     = System.Text.Json.JsonSerializer.Serialize(avgRatingByAgence.Select(x => Math.Round(x.AvgNote, 1)).ToArray());
            ViewBag.RecentReservations   = recentReservations;

            return View();
        }
    }
}
