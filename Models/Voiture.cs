using EasyCars.Models.Enums;

namespace EasyCars.Models
{
    public class Voiture
    {
        public int Id { get; set; }
        public string Modele { get; set; } = string.Empty;
        public decimal PrixParJour { get; set; }
        public StatutVoiture Statut { get; set; } = StatutVoiture.Disponible;

        // FK
        public int AgenceId { get; set; }
        public Agence Agence { get; set; } = null!;

        // Navigation
        public ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();
        public ICollection<Feedback> Feedbacks { get; set; } = new List<Feedback>();
    }
}
