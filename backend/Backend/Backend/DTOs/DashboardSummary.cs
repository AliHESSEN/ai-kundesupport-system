namespace Backend.DTOs
{
    /// <summary>
    /// Returneres av GET /admin/dashboard – sammendrag av nøkkel-statistikk.
    /// </summary>
    public class DashboardSummary
    {
        public int OpenCases { get; init; }
        public int ClosedCases { get; init; }
        public double AvgResolutionTimeHours { get; init; }
        public int AgentsOnline { get; init; }
    }
}
