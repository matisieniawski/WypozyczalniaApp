using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WypozyczalniaApp.Models
{
    public class ModelPojazdu
    {
        [Key]
        [Column("model_id")]
        public int ModelId { get; set; }

        [Column("producent_id")]
        public int ProducentId { get; set; }
        public Producent Producent { get; set; } = null!;

        [Column("nazwa_modelu")]
        public string NazwaModelu { get; set; } = string.Empty;

        [Column("cena_za_dobe")]
        public decimal CenaZaDobe { get; set; }
    }
}