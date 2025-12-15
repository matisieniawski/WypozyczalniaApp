using Microsoft.EntityFrameworkCore;
using WypozyczalniaApp.Models;

namespace WypozyczalniaApp.Data
{
    public class WypozyczalniaDbContext : DbContext
    {
        public WypozyczalniaDbContext(DbContextOptions<WypozyczalniaDbContext> options)
            : base(options) { }

        public DbSet<Pojazd> Pojazdy { get; set; } = null!;
        public DbSet<Producent> Producenci { get; set; } = null!;
        public DbSet<ModelPojazdu> ModelePojazdow { get; set; } = null!;
        public DbSet<Klient> Klienci { get; set; } = null!;
        public DbSet<Lokalizacja> Lokalizacje { get; set; } = null!;
        public DbSet<Pracownik> Pracownicy { get; set; } = null!;
        public DbSet<Rola> Role { get; set; } = null!;
        public DbSet<PracownikRola> PracownicyRole { get; set; } = null!;
        public DbSet<Wynajem> Wynajmy { get; set; } = null!;
        public DbSet<Serwisowanie> Serwisowanie { get; set; } = null!;
        public DbSet<Faktura> Faktury { get; set; } = null!;
        public DbSet<ModelPopularnosc> PopularnoscModeli { get; set; } = null!;


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            
            modelBuilder.Entity<Pojazd>().ToTable("pojazdy");
            modelBuilder.Entity<Producent>().ToTable("producenci");
            modelBuilder.Entity<ModelPojazdu>().ToTable("modelepojazdow");
            modelBuilder.Entity<Klient>().ToTable("klienci");
            modelBuilder.Entity<Lokalizacja>().ToTable("lokalizacje");
            modelBuilder.Entity<Pracownik>().ToTable("pracownicy");
            modelBuilder.Entity<Rola>().ToTable("role");
            modelBuilder.Entity<Wynajem>().ToTable("wynajmy");
            modelBuilder.Entity<Serwisowanie>().ToTable("serwisowanie");
            modelBuilder.Entity<Faktura>().ToTable("faktury");
            modelBuilder.Entity<ModelPopularnosc>().HasNoKey().ToView("widok_popularnosc_modeli");

           
            modelBuilder.Entity<PracownikRola>()
                .ToTable("pracownicyrole")
                .HasKey(pr => new { pr.PracownikId, pr.RolaId });

            modelBuilder.Entity<PracownikRola>()
                .HasOne(pr => pr.Pracownik)
                .WithMany(p => p.PracownicyRole)
                .HasForeignKey(pr => pr.PracownikId);

            modelBuilder.Entity<PracownikRola>()
                .HasOne(pr => pr.Rola)
                .WithMany(r => r.PracownicyRole)
                .HasForeignKey(pr => pr.RolaId);

        }
    }
}