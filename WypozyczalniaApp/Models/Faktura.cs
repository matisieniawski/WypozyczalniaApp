using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WypozyczalniaApp.Models
{
    public class Faktura
    {
        [Key]
        [Column("faktura_id")]
        public int FakturaId { get; set; }

        [Column("wynajem_id")]
        public int WynajemId { get; set; }
        public Wynajem Wynajem { get; set; } = null!;

        [Column("data_wystawienia")]
        public DateTime DataWystawienia { get; set; }

        [Column("kwota_brutto")]
        public decimal KwotaBrutto { get; set; }

        [Column("status_platnosci")]
        public string StatusPlatnosci { get; set; } = "Nieoplacona";
    }
}