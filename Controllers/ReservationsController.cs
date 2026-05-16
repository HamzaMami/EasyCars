using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using EasyCars.Data;
using EasyCars.Models;
using EasyCars.Services;
using EasyCars.Hubs;
using Microsoft.AspNetCore.SignalR;

using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

namespace EasyCars.Controllers
{
    [Authorize]
    public class ReservationsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILoyaltyService _loyaltyService;
        private readonly IHubContext<NotificationHub> _hubContext;

        public ReservationsController(ApplicationDbContext context, ILoyaltyService loyaltyService, IHubContext<NotificationHub> hubContext)
        {
            _context = context;
            _loyaltyService = loyaltyService;
            _hubContext = hubContext;
        }

        // GET: Reservations
        public async Task<IActionResult> Index()
        {
            var applicationDbContext = _context.Reservations.Include(r => r.Utilisateur).Include(r => r.Voiture);
            return View(await applicationDbContext.ToListAsync());
        }

        // GET: Reservations/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var reservation = await _context.Reservations
                .Include(r => r.Utilisateur)
                .Include(r => r.Voiture)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (reservation == null)
            {
                return NotFound();
            }

            return View(reservation);
        }

        // GET: Reservations/Create
        public async Task<IActionResult> Create(int? voitureId)
        {
            var voitures = await _context.Voitures.ToListAsync();
            ViewData["VoitureId"] = new SelectList(voitures, "Id", "Modele", voitureId);
            
            // Build PriceMap for JS dynamic cost calculation
            var priceMap = voitures.ToDictionary(v => v.Id, v => v.PrixParJour);
            ViewBag.PriceMap = System.Text.Json.JsonSerializer.Serialize(priceMap);

            return View(new Reservation { 
                DateDebut = DateTime.Today,
                DateFin = DateTime.Today.AddDays(1)
            });
        }

        // POST: Reservations/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,DateDebut,DateFin,VoitureId")] Reservation reservation)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            reservation.UtilisateurId = userId;
            ModelState.Remove("UtilisateurId"); // Remove validation error if any
            ModelState.Remove("Voiture");
            ModelState.Remove("Utilisateur");

            if (ModelState.IsValid)
            {
                var voiture = await _context.Voitures.FindAsync(reservation.VoitureId);
                var utilisateur = await _context.Users.FindAsync(reservation.UtilisateurId);

                if (voiture != null && utilisateur != null)
                {
                    // Business Logic: Calculate amount
                    var days = (reservation.DateFin - reservation.DateDebut).Days;
                    if (days <= 0) days = 1;
                    
                    var baseAmount = days * voiture.PrixParJour;
                    
                    // Event: OnReservationCreated - Calculate dynamic discount
                    reservation.RemiseAppliquee = _loyaltyService.CalculateDiscount(utilisateur, baseAmount);
                    reservation.MontantTotal = baseAmount - reservation.RemiseAppliquee;
                    reservation.Statut = Models.Enums.StatutReservation.EnAttente;

                    _context.Add(reservation);
                    await _context.SaveChangesAsync();
                    
                    return RedirectToAction(nameof(Index));
                }
            }
            
            var voitures = await _context.Voitures.ToListAsync();
            ViewData["VoitureId"] = new SelectList(voitures, "Id", "Modele", reservation.VoitureId);
            var priceMap = voitures.ToDictionary(v => v.Id, v => v.PrixParJour);
            ViewBag.PriceMap = System.Text.Json.JsonSerializer.Serialize(priceMap);

            return View(reservation);
        }

        // GET: Reservations/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var reservation = await _context.Reservations.FindAsync(id);
            if (reservation == null)
            {
                return NotFound();
            }
            ViewData["UtilisateurId"] = new SelectList(_context.Users, "Id", "Email");
            ViewData["VoitureId"] = new SelectList(_context.Voitures, "Id", "Modele", reservation.VoitureId);
            return View(reservation);
        }

        // POST: Reservations/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,DateDebut,DateFin,MontantTotal,RemiseAppliquee,Statut,VoitureId,UtilisateurId")] Reservation reservation)
        {
            if (id != reservation.Id)
            {
                return NotFound();
            }

            ModelState.Remove("Voiture");
            ModelState.Remove("Utilisateur");

            if (ModelState.IsValid)
            {
                try
                {
                    var original = await _context.Reservations.AsNoTracking().FirstOrDefaultAsync(r => r.Id == id);
                    _context.Update(reservation);
                    await _context.SaveChangesAsync();

                    // Event: OnStatusChanged - Trigger SignalR Notification
                    if (original != null && original.Statut != reservation.Statut)
                    {
                        var message = $"Le statut de votre réservation #{reservation.Id} est passé à {reservation.Statut}.";
                        await _hubContext.Clients.User(reservation.UtilisateurId).SendAsync("ReceiveNotification", message);
                        
                        // If validated, update loyalty points and car status
                        if (reservation.Statut == Models.Enums.StatutReservation.Confirmee)
                        {
                            var user = await _context.Users.FindAsync(reservation.UtilisateurId);
                            if (user != null)
                            {
                                user.PointsFidelite += _loyaltyService.CalculatePoints(reservation.MontantTotal);
                                user.NiveauMembre = _loyaltyService.UpdateMembershipLevel(user.PointsFidelite);
                                _context.Update(user);
                            }

                            var voiture = await _context.Voitures.FindAsync(reservation.VoitureId);
                            if (voiture != null)
                            {
                                voiture.Statut = Models.Enums.StatutVoiture.Louee;
                                _context.Update(voiture);
                            }

                            await _context.SaveChangesAsync();
                        }
                    }
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ReservationExists(reservation.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            ViewData["UtilisateurId"] = new SelectList(_context.Users, "Id", "Email");
            ViewData["VoitureId"] = new SelectList(_context.Voitures, "Id", "Modele", reservation.VoitureId);
            return View(reservation);
        }

        // GET: Reservations/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var reservation = await _context.Reservations
                .Include(r => r.Utilisateur)
                .Include(r => r.Voiture)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (reservation == null)
            {
                return NotFound();
            }

            return View(reservation);
        }

        // POST: Reservations/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var reservation = await _context.Reservations.FindAsync(id);
            if (reservation != null)
            {
                _context.Reservations.Remove(reservation);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool ReservationExists(int id)
        {
            return _context.Reservations.Any(e => e.Id == id);
        }
    }
}
