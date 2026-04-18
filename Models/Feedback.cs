namespace EasyCars.Models
{
    public class Feedback
    {
        public int Id { get; set; }
        public int Note { get; set; }          // 1 to 5
        public string Commentaire { get; set; } = string.Empty;
        public DateTime Date { get; set; } = DateTime.Now;

        // FKs (VoitureId OR AgenceId — one is optional)
        public int UtilisateurId { get; set; }
        public Utilisateur Utilisateur { get; set; } = null!;

        public int? VoitureId { get; set; }
        public Voiture? Voiture { get; set; }

        public int? AgenceId { get; set; }
        public Agence? Agence { get; set; }
    }
}
