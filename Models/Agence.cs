namespace EasyCars.Models
{
    public class Agence
    {
        public int Id { get; set; }
        public string Nom { get; set; } = string.Empty;
        public string Ville { get; set; } = string.Empty;
        public string Adresse { get; set; } = string.Empty;
        public decimal ChiffreAffairesTotal { get; set; }

        // Navigation
        public ICollection<Voiture> Voitures { get; set; } = new List<Voiture>();
        public ICollection<Feedback> Feedbacks { get; set; } = new List<Feedback>();
    }
}
