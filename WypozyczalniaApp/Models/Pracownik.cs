using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WypozyczalniaApp.Models
{
    public class Pracownik
    {
        [Key]
        [Column("pracownik_id")]
        public int PracownikId { get; set; }

        [Column("imie")]
        public string Imie { get; set; } = string.Empty;

        [Column("nazwisko")]
        public string Nazwisko { get; set; } = string.Empty;

        [Column("login")]
        public string Login { get; set; } = string.Empty;

        [Column("haslo_hash")]
        public string HasloHash { get; set; } = string.Empty;

        public ICollection<PracownikRola> PracownicyRole { get; set; } = new List<PracownikRola>();
    }
}