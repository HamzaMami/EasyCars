using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using EasyCars.Data;
using EasyCars.Models;

using Microsoft.AspNetCore.Authorization;

namespace EasyCars.Controllers
{
    [Authorize]
    public class VoituresController : Controller
    {
        private readonly ApplicationDbContext _context;

        public VoituresController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Voitures
        [AllowAnonymous]
        public async Task<IActionResult> Index()
        {
            var applicationDbContext = _context.Voitures.Include(v => v.Agence);
            return View(await applicationDbContext.ToListAsync());
        }

        // GET: Voitures/Search?ville=Paris&debut=2026-06-01&fin=2026-06-07
        [AllowAnonymous]
        public async Task<IActionResult> Search(string? ville, DateTime? debut, DateTime? fin, string? modele)
        {
            var query = _context.Voitures.Include(v => v.Agence).AsQueryable();

            if (!string.IsNullOrWhiteSpace(ville))
                query = query.Where(v => v.Agence.Ville.Contains(ville) || v.Agence.Nom.Contains(ville));

            if (!string.IsNullOrWhiteSpace(modele))
                query = query.Where(v => v.Modele.Contains(modele));

            // OnDateChanged: filter out cars reserved in the requested window
            if (debut.HasValue && fin.HasValue)
            {
                var bookedIds = await _context.Reservations
                    .Where(r => r.DateDebut < fin && r.DateFin > debut
                             && r.Statut != Models.Enums.StatutReservation.Terminee)
                    .Select(r => r.VoitureId)
                    .Distinct()
                    .ToListAsync();

                query = query.Where(v => !bookedIds.Contains(v.Id));
            }

            ViewBag.Ville  = ville;
            ViewBag.Debut  = debut?.ToString("yyyy-MM-dd");
            ViewBag.Fin    = fin?.ToString("yyyy-MM-dd");
            ViewBag.Modele = modele;

            return View("Index", await query.ToListAsync());
        }

        // GET: Voitures/Details/5
        [AllowAnonymous]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var voiture = await _context.Voitures
                .Include(v => v.Agence)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (voiture == null)
            {
                return NotFound();
            }

            return View(voiture);
        }

        // GET: Voitures/Create
        [Authorize(Roles = "SuperAdmin,AgenceAdmin")]
        public IActionResult Create()
        {
            ViewData["AgenceId"] = new SelectList(_context.Agences, "Id", "Nom");
            return View();
        }

        // POST: Voitures/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "SuperAdmin,AgenceAdmin")]
        public async Task<IActionResult> Create([Bind("Id,Modele,PrixParJour,Statut,AgenceId")] Voiture voiture)
        {
            ModelState.Remove("Agence");
            if (ModelState.IsValid)
            {
                _context.Add(voiture);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["AgenceId"] = new SelectList(_context.Agences, "Id", "Nom", voiture.AgenceId);
            return View(voiture);
        }

        // GET: Voitures/Edit/5
        [Authorize(Roles = "SuperAdmin,AgenceAdmin")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var voiture = await _context.Voitures.FindAsync(id);
            if (voiture == null)
            {
                return NotFound();
            }
            ViewData["AgenceId"] = new SelectList(_context.Agences, "Id", "Nom", voiture.AgenceId);
            return View(voiture);
        }

        // POST: Voitures/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "SuperAdmin,AgenceAdmin")]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Modele,PrixParJour,Statut,AgenceId")] Voiture voiture)
        {
            if (id != voiture.Id)
            {
                return NotFound();
            }

            ModelState.Remove("Agence");
            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(voiture);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!VoitureExists(voiture.Id))
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
            ViewData["AgenceId"] = new SelectList(_context.Agences, "Id", "Nom", voiture.AgenceId);
            return View(voiture);
        }

        // GET: Voitures/Delete/5
        [Authorize(Roles = "SuperAdmin,AgenceAdmin")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var voiture = await _context.Voitures
                .Include(v => v.Agence)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (voiture == null)
            {
                return NotFound();
            }

            return View(voiture);
        }

        // POST: Voitures/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "SuperAdmin,AgenceAdmin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var voiture = await _context.Voitures.FindAsync(id);
            if (voiture != null)
            {
                _context.Voitures.Remove(voiture);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool VoitureExists(int id)
        {
            return _context.Voitures.Any(e => e.Id == id);
        }
    }
}
