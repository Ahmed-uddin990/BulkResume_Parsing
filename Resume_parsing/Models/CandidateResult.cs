using System.ComponentModel.DataAnnotations.Schema;

namespace Resume_parsing.Models
{


    public class CandidateResult
    {
        public int Id { get; set; }
        public Guid JobId { get; set; }
        public string Message { get; set; }
        public string Status { get; set; }
        public string FileKey { get; set; }
        public string Error { get; set; }
        public string Link { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public string Mobile_Number { get; set; }
        public string Skills { get; set; }
        public string College_Name { get; set; }
        public string Degree { get; set; }
        public string Designation { get; set; }
        public string Experience { get; set; }
        public string Company_Names { get; set; }
        public int No_Of_Pages { get; set; }
        public decimal Total_Experience { get; set; }
        public DateTime? CreatedDate { get; set; }
    }
}
