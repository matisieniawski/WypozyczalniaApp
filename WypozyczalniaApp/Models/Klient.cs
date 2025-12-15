using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WypozyczalniaApp.Models
{
    public class Klient
    {
        [Key]
        [Column("klient_id")]
        public int KlientId { get; set; }

        [Required(ErrorMessage = "Imię jest wymagane.")]
        [Column("imie")]
        public string Imie { get; set; } = string.Empty;

        [Required(ErrorMessage = "Nazwisko jest wymagane.")]
        [Column("nazwisko")]
        public string Nazwisko { get; set; } = string.Empty;

        [Required(ErrorMessage = "Adres email jest wymagany.")]
        [EmailAddress(ErrorMessage = "Nieprawidłowy format adresu email.")]
        [Column("email")]
        public string Email { get; set; } = string.Empty;

        [Column("numer_telefonu")]
        public string? NumerTelefonu { get; set; }
    }
}