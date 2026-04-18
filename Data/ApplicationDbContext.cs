using EasyCars.Models;
using EasyCars.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace EasyCars.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options) { }

        // ── DbSets ──────────────────────────────────────────────
        public DbSet<Agence> Agences { get; set; }
        public DbSet<Voiture> Voitures { get; set; }
        public DbSet<Utilisateur> Utilisateurs { get; set; }
        public DbSet<Reservation> Reservations { get; set; }
        public DbSet<Feedback> Feedbacks { get; set; }

        // ── Model Configuration ─────────────────────────────────
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ── Agence ──────────────────────────────────────────
            modelBuilder.Entity<Agence>(entity =>
            {
                entity.HasKey(a => a.Id);
                entity.Property(a => a.Nom).IsRequired().HasMaxLength(100);
                entity.Property(a => a.Ville).IsRequired().HasMaxLength(100);
                entity.Property(a => a.Adresse).HasMaxLength(250);
                entity.Property(a => a.ChiffreAffairesTotal)
                      .HasColumnType("decimal(18,2)");
            });

            // ── Voiture ─────────────────────────────────────────
            modelBuilder.Entity<Voiture>(entity =>
            {
                entity.HasKey(v => v.Id);
                entity.Property(v => v.Modele).IsRequired().HasMaxLength(100);
                entity.Property(v => v.PrixParJour)
                      .IsRequired()
                      .HasColumnType("decimal(18,2)");
                entity.Property(v => v.Statut)
                      .HasConversion<string>();   // stores "Disponible" etc. in DB

                // Voiture → Agence (many-to-one)
                entity.HasOne(v => v.Agence)
                      .WithMany(a => a.Voitures)
                      .HasForeignKey(v => v.AgenceId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // ── Utilisateur ─────────────────────────────────────
            modelBuilder.Entity<Utilisateur>(entity =>
            {
                entity.HasKey(u => u.Id);
                entity.Property(u => u.Nom).IsRequired().HasMaxLength(100);
                entity.Property(u => u.Email).IsRequired().HasMaxLength(200);
                entity.HasIndex(u => u.Email).IsUnique();
                entity.Property(u => u.PointsFidelite).HasDefaultValue(0);
                entity.Property(u => u.NiveauMembre)
                      .HasConversion<string>();
            });

            // ── Reservation ─────────────────────────────────────
            modelBuilder.Entity<Reservation>(entity =>
            {
                entity.HasKey(r => r.Id);
                entity.Property(r => r.MontantTotal)
                      .HasColumnType("decimal(18,2)");
                entity.Property(r => r.RemiseAppliquee)
                      .HasColumnType("decimal(18,2)")
                      .HasDefaultValue(0);
                entity.Property(r => r.Statut)
                      .HasConversion<string>();

                // Reservation → Voiture (many-to-one)
                entity.HasOne(r => r.Voiture)
                      .WithMany(v => v.Reservations)
                      .HasForeignKey(r => r.VoitureId)
                      .OnDelete(DeleteBehavior.Restrict);

                // Reservation → Utilisateur (many-to-one)
                entity.HasOne(r => r.Utilisateur)
                      .WithMany(u => u.Reservations)
                      .HasForeignKey(r => r.UtilisateurId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // ── Feedback ────────────────────────────────────────
            modelBuilder.Entity<Feedback>(entity =>
            {
                entity.HasKey(f => f.Id);
                entity.Property(f => f.Note).IsRequired();
                entity.Property(f => f.Commentaire).HasMaxLength(1000);
                entity.Property(f => f.Date).HasDefaultValueSql("GETDATE()");

                // Note range constraint (1–5)
                entity.ToTable(t => t.HasCheckConstraint("CK_Feedback_Note",
                    "[Note] >= 1 AND [Note] <= 5"));

                // Feedback → Utilisateur
                entity.HasOne(f => f.Utilisateur)
                      .WithMany(u => u.Feedbacks)
                      .HasForeignKey(f => f.UtilisateurId)
                      .OnDelete(DeleteBehavior.Restrict);

                // Feedback → Voiture (optional)
                entity.HasOne(f => f.Voiture)
                      .WithMany(v => v.Feedbacks)
                      .HasForeignKey(f => f.VoitureId)
                      .IsRequired(false)
                      .OnDelete(DeleteBehavior.SetNull);

                // Feedback → Agence (optional)
                entity.HasOne(f => f.Agence)
                      .WithMany(a => a.Feedbacks)
                      .HasForeignKey(f => f.AgenceId)
                      .IsRequired(false)
                      .OnDelete(DeleteBehavior.SetNull);
            });
        }
    }
}