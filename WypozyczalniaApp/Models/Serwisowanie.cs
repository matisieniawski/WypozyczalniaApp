using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WypozyczalniaApp.Models
{
    public class Serwisowanie
    {
        [Key]
        [Column("serwis_id")]
        public int SerwisId { get; set; }

        [Column("pojazd_id")]
        public int PojazdId { get; set; }
        public Pojazd Pojazd { get; set; } = null!;

        [Column("data_serwisu")]
        public DateTime DataSerwisu { get; set; }

        [Column("opis")]
        public string Opis { get; set; } = string.Empty;
    }
}