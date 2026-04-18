using EasyCars.Models.Enums;

namespace EasyCars.Models
{
    public class Reservation
    {
        public int Id { get; set; }
        public DateTime DateDebut { get; set; }
        public DateTime DateFin { get; set; }
        public decimal MontantTotal { get; set; }
        public decimal RemiseAppliquee { get; set; } = 0;
        public StatutReservation Statut { get; set; } = StatutReservation.EnAttente;

        // FKs
        public int VoitureId { get; set; }
        public Voiture Voiture { get; set; } = null!;

        public int UtilisateurId { get; set; }
        public Utilisateur Utilisateur { get; set; } = null!;
    }

}
