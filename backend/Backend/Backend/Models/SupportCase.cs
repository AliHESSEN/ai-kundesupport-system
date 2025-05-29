namespace Backend.Models
{
    // Representerer en supportsak
    public class SupportCase
    {

        public int Id { get; set; } // Unik ID for supportsaken
        public string Title { get; set; } = default!; // Tittel på saken (kort beskrivelse)
        public string Description { get; set; } = default!; // Detaljert beskrivelse av problemet
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow; // Når saken ble opprettet (settes automatisk til nåtid)
        public string Status { get; set; } = "Open";   // Status på saken (f.eks. "Open", "Closed")



    }
}
