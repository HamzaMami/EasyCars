using EasyCars.Models;
using EasyCars.Models.Enums;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace EasyCars.Data
{
    public class ApplicationDbContext : IdentityDbContext<Utilisateur>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options) { }

        public DbSet<Agence> Agences { get; set; }
        public DbSet<Voiture> Voitures { get; set; }
        public DbSet<Reservation> Reservations { get; set; }
        public DbSet<Feedback> Feedbacks { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Agence>(entity =>
            {
                entity.HasKey(a => a.Id);
                entity.Property(a => a.Nom).IsRequired().HasMaxLength(100);
                entity.Property(a => a.Ville).IsRequired().HasMaxLength(100);
                entity.Property(a => a.Adresse).HasMaxLength(250);
                entity.Property(a => a.ChiffreAffairesTotal)
                      .HasColumnType("decimal(18,2)");
            });

            modelBuilder.Entity<Voiture>(entity =>
            {
                entity.HasKey(v => v.Id);
                entity.Property(v => v.Modele).IsRequired().HasMaxLength(100);
                entity.Property(v => v.PrixParJour)
                      .IsRequired()
                      .HasColumnType("decimal(18,2)");
                entity.Property(v => v.Statut)
                      .HasConversion<string>();

                entity.HasOne(v => v.Agence)
                      .WithMany(a => a.Voitures)
                      .HasForeignKey(v => v.AgenceId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

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

                entity.HasOne(r => r.Voiture)
                      .WithMany(v => v.Reservations)
                      .HasForeignKey(r => r.VoitureId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(r => r.Utilisateur)
                      .WithMany(u => u.Reservations)
                      .HasForeignKey(r => r.UtilisateurId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<Feedback>(entity =>
            {
                entity.HasKey(f => f.Id);
                entity.Property(f => f.Note).IsRequired();
                entity.Property(f => f.Commentaire).HasMaxLength(1000);
                entity.Property(f => f.Date).HasDefaultValueSql("GETDATE()");

                entity.ToTable(t => t.HasCheckConstraint("CK_Feedback_Note",
                    "[Note] >= 1 AND [Note] <= 5"));

                entity.HasOne(f => f.Utilisateur)
                      .WithMany(u => u.Feedbacks)
                      .HasForeignKey(f => f.UtilisateurId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(f => f.Voiture)
                      .WithMany(v => v.Feedbacks)
                      .HasForeignKey(f => f.VoitureId)
                      .IsRequired(false)
                      .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(f => f.Agence)
                      .WithMany(a => a.Feedbacks)
                      .HasForeignKey(f => f.AgenceId)
                      .IsRequired(false)
                      .OnDelete(DeleteBehavior.SetNull);
            });
        }
    }
}