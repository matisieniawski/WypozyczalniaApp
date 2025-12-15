using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WypozyczalniaApp.Models
{
    public class PracownikRola
    {
        [Column("pracownik_id")]
        public int PracownikId { get; set; }
        public Pracownik Pracownik { get; set; } = null!;

        [Column("rola_id")]
        public int RolaId { get; set; }
        public Rola Rola { get; set; } = null!;
    }
}