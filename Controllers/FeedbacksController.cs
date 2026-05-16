using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using EasyCars.Data;
using EasyCars.Models;
using EasyCars.Hubs;
using Microsoft.AspNetCore.SignalR;

using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace EasyCars.Controllers
{
    [Authorize]
    public class FeedbacksController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IHubContext<NotificationHub> _hubContext;

        public FeedbacksController(ApplicationDbContext context, IHubContext<NotificationHub> hubContext)
        {
            _context = context;
            _hubContext = hubContext;
        }

        // GET: Feedbacks
        [AllowAnonymous]
        public async Task<IActionResult> Index()
        {
            var applicationDbContext = _context.Feedbacks.Include(f => f.Agence).Include(f => f.Utilisateur).Include(f => f.Voiture);
            return View(await applicationDbContext.ToListAsync());
        }

        // GET: Feedbacks/Details/5
        [AllowAnonymous]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var feedback = await _context.Feedbacks
                .Include(f => f.Agence)
                .Include(f => f.Utilisateur)
                .Include(f => f.Voiture)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (feedback == null)
            {
                return NotFound();
            }

            return View(feedback);
        }

        // GET: Feedbacks/Create
        public IActionResult Create()
        {
            ViewData["AgenceId"] = new SelectList(_context.Agences, "Id", "Nom");
            ViewData["VoitureId"] = new SelectList(_context.Voitures, "Id", "Modele");
            return View();
        }

        // POST: Feedbacks/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Note,Commentaire,VoitureId,AgenceId")] Feedback feedback)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            feedback.UtilisateurId = userId;
            ModelState.Remove("UtilisateurId");
            ModelState.Remove("Utilisateur");

            if (ModelState.IsValid)
            {
                feedback.Date = DateTime.Now;
                _context.Add(feedback);
                await _context.SaveChangesAsync();

                // Event: OnFeedbackSubmitted - Broadcast update
                await _hubContext.Clients.All.SendAsync("FeedbackSubmitted", new {
                    user = _context.Users.Find(feedback.UtilisateurId)?.Email,
                    note = feedback.Note,
                    comment = feedback.Commentaire
                });

                return RedirectToAction(nameof(Index));
            }
            ViewData["AgenceId"] = new SelectList(_context.Agences, "Id", "Nom", feedback.AgenceId);
            ViewData["VoitureId"] = new SelectList(_context.Voitures, "Id", "Modele", feedback.VoitureId);
            return View(feedback);
        }

        // GET: Feedbacks/Edit/5
        [Authorize(Roles = "SuperAdmin,AgenceAdmin")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var feedback = await _context.Feedbacks.FindAsync(id);
            if (feedback == null)
            {
                return NotFound();
            }
            ViewData["AgenceId"] = new SelectList(_context.Agences, "Id", "Nom", feedback.AgenceId);
            ViewData["UtilisateurId"] = new SelectList(_context.Users, "Id", "Email");
            ViewData["VoitureId"] = new SelectList(_context.Voitures, "Id", "Modele", feedback.VoitureId);
            return View(feedback);
        }

        // POST: Feedbacks/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "SuperAdmin,AgenceAdmin")]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Note,Commentaire,Date,UtilisateurId,VoitureId,AgenceId")] Feedback feedback)
        {
            if (id != feedback.Id)
            {
                return NotFound();
            }

            ModelState.Remove("Utilisateur");
            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(feedback);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!FeedbackExists(feedback.Id))
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
            ViewData["AgenceId"] = new SelectList(_context.Agences, "Id", "Nom", feedback.AgenceId);
            ViewData["UtilisateurId"] = new SelectList(_context.Users, "Id", "Email");
            ViewData["VoitureId"] = new SelectList(_context.Voitures, "Id", "Modele", feedback.VoitureId);
            return View(feedback);
        }

        // GET: Feedbacks/Delete/5
        [Authorize(Roles = "SuperAdmin,AgenceAdmin")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var feedback = await _context.Feedbacks
                .Include(f => f.Agence)
                .Include(f => f.Utilisateur)
                .Include(f => f.Voiture)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (feedback == null)
            {
                return NotFound();
            }

            return View(feedback);
        }

        // POST: Feedbacks/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "SuperAdmin,AgenceAdmin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var feedback = await _context.Feedbacks.FindAsync(id);
            if (feedback != null)
            {
                _context.Feedbacks.Remove(feedback);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool FeedbackExists(int id)
        {
            return _context.Feedbacks.Any(e => e.Id == id);
        }
    }
}
