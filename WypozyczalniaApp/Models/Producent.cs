using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WypozyczalniaApp.Models
{
    public class Producent
    {
        [Key]
        [Column("producent_id")]
        public int ProducentId { get; set; }

        [Column("nazwa")]
        public string Nazwa { get; set; } = string.Empty;

    }
}