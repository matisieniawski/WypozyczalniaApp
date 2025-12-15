using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WypozyczalniaApp.Models
{
    public class Pojazd
    {
        [Key]
        [Column("pojazd_id")]
        public int PojazdId { get; set; }

        [Column("numer_vin")]
        [StringLength(17)]
        public string NumerVin { get; set; } = string.Empty;

        [Column("status")]
        public string Status { get; set; } = "Dostepny";

        [Column("numer_rejestracyjny")]
        public string? NumerRejestracyjny { get; set; }

        [Column("data_dodania")]
        public DateTime DataDodania { get; set; }

        [Column("data_ostatniego_serwisu")]
        public DateTime? DataOstatniegoSerwisu { get; set; }
        
        [Column("model_id")]
        public int ModelId { get; set; }
        public ModelPojazdu Model { get; set; } = null!;
    }
}