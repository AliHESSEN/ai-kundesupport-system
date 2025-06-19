namespace Backend.Models
{
    public class AuditLog
    {
        public int Id { get; set; }

        public string? UserId { get; set; }  // Hvem utførte handlingen

        public string? Role { get; set; }    // Rollen til brukeren (valgfritt, men nyttig)

        public string Action { get; set; } = null!;  // Hva som ble gjort (f.eks. "Oppdaterte status")

        public int? CaseId { get; set; }     // Hvilken sak det gjelder (valgfritt, men viktig for kontekst)

        public DateTime Timestamp { get; set; }  // Når handlingen ble gjort

        public string? AdditionalInfo { get; set; }  // Ekstra detaljer som "endret fra X til Y"

        public string? Details { get; set; } // fritekstfelt for logging

    }
}
