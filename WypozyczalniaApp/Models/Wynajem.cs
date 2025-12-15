using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WypozyczalniaApp.Models
{
    public class Wynajem
    {
        [Key]
        [Column("wynajem_id")]
        public int WynajemId { get; set; }

        [Column("klient_id")]
        public int KlientId { get; set; }
        public Klient Klient { get; set; } = null!;

        [Column("pojazd_id")]
        public int PojazdId { get; set; }
        public Pojazd Pojazd { get; set; } = null!;

        [Column("pracownik_id")]
        public int PracownikId { get; set; }
        public Pracownik Pracownik { get; set; } = null!;

        [Column("lokalizacja_odbioru_id")]
        public int LokalizacjaOdbioruId { get; set; }
        public Lokalizacja LokalizacjaOdbioru { get; set; } = null!;

        [Column("data_wypozyczenia")]
        public DateTime DataWypozyczenia { get; set; }

        [Column("data_zwrotu_planowana")]
        public DateTime DataZwrotuPlanowana { get; set; }

        [Column("data_zwrotu_rzeczywista")]
        public DateTime? DataZwrotuRzeczywista { get; set; }

        [Column("koszt_calkowity")]
        public decimal? KosztCalkowity { get; set; }
    }
}