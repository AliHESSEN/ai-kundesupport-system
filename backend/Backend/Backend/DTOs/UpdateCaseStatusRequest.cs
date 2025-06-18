namespace Backend.DTOs
{
    

    // Her tillater jeg kun å sende inn "Status", og ikke hele supportsaken. Jeg gjør dette for 
    public class UpdateCaseStatusRequest
    {
        public string Status { get; set; } = string.Empty; //statusen som SupportStaff/Admin ønsker å sette (f.eks. "Open", "Closed", "In Progress" osv)


    }



}
