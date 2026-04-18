using Microsoft.AspNetCore.Identity;
using EasyCars.Models.Enums;

namespace EasyCars.Models
{
    public class Utilisateur : IdentityUser
    {
        public string Nom { get; set; } = string.Empty;
        public int PointsFidelite { get; set; } = 0;
        public NiveauMembre NiveauMembre { get; set; } = NiveauMembre.Bronze;

        // Navigation
        public ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();
        public ICollection<Feedback> Feedbacks { get; set; } = new List<Feedback>();
    }
}