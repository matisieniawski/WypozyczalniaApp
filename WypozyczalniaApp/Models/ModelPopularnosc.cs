using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;

namespace WypozyczalniaApp.Models
{
    [Keyless]
    public class ModelPopularnosc
    {
        [Column("model")]
        public string Model { get; set; } = string.Empty;

        [Column("liczba_wynajmow")]
        public int LiczbaWynajmow { get; set; }
    }
}