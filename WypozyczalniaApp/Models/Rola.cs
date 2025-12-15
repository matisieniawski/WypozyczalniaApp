using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WypozyczalniaApp.Models
{
    public class Rola
    {
        [Key]
        [Column("rola_id")]
        public int RolaId { get; set; }

        [Column("nazwa_roli")]
        public string NazwaRoli { get; set; } = string.Empty;
        public ICollection<PracownikRola> PracownicyRole { get; set; } = new List<PracownikRola>();
    }
}