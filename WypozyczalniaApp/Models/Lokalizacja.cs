using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WypozyczalniaApp.Models
{
    public class Lokalizacja
    {
        [Key]
        [Column("lokalizacja_id")]
        public int LokalizacjaId { get; set; }

        [Column("nazwa")]
        public string Nazwa { get; set; } = string.Empty;

        [Column("adres")]
        public string Adres { get; set; } = string.Empty;
    }
}