using System.ComponentModel.DataAnnotations;

namespace Backend.Models
{
    // Representerer en supportsak
    public class SupportCase
    {

        public int Id { get; set; } // Unik ID for supportsaken


        [Required(ErrorMessage = "Title er påkrevd")]
        [MaxLength(100, ErrorMessage = "Title kan maks være 100 tegn")]
        public string Title { get; set; } = default!; // Tittel på saken (kort beskrivelse)

        [Required(ErrorMessage = "Description er påkrevd")]
        public string Description { get; set; } = default!; // Detaljert beskrivelse av problemet

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow; // Når saken ble opprettet (settes automatisk til nåtid)

        public DateTime? ClosedAt { get; set; } // når saken ble lukket


        [Required(ErrorMessage = "Status er påkrevd")]
        public string Status { get; set; } = "Open";   // Status på saken (f.eks. "Open", "Closed")


        // bruker disse for hvem som har oppretter saker

        public string? CreatedById { get; set; } // Bruker-ID som opprettet saken bruker ? for å tillate null verdi
        public ApplicationUser? CreatedBy { get; set; } // Navigasjonsegenskap (for relasjon)




    }
}
