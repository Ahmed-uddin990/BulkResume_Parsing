
//using System.ComponentModel.DataAnnotations;
//using System.ComponentModel.DataAnnotations.Schema;
//using System.Text.Json.Serialization;

//namespace Resume_parsing.Models
//{
//    [Table("Job_Status")]
//    public class JobStatus
//    {
//        [Key]
//        [Column("Id")]
//        public int Id { get; set; }

//        [Column("Job_Id")]
//        public int Job_Id { get; set; }

//        [Column("Total")]
//        public int? Total { get; set; }

//        [Column("Passed")]
//        public int? Passed { get; set; }

//        [Column("Failed")]
//        public int? Failed { get; set; }

//        [Column("Processed")]
//        public int? Processed { get; set; }

//        [Column("CreatedDate")]
//        public DateTime CreatedDate { get; set; }

//        // ✅ Correctly mapped navigation property
//        [ForeignKey("Job_Id")]
//        public Job_migrate_result JobResult { get; set; }
//    }

//    // Your API response models remain the same
//    public class StartJobApiResponse
//    {
//        [JsonPropertyName("status")]
//        public bool Status { get; set; }
//        [JsonPropertyName("data")]
//        public string Data { get; set; }
//    }

//    public class CvStatsData
//    {
//        [JsonPropertyName("total")]
//        public int Total { get; set; }
//        [JsonPropertyName("passed")]
//        public int Passed { get; set; }
//        [JsonPropertyName("failed")]
//        public int Failed { get; set; }
//        [JsonPropertyName("processed")]
//        public int Processed { get; set; }
//        [JsonPropertyName("failed_names")]
//        public List<string> FailedNames { get; set; }
//    }

//    public class CvStatsApiResponse
//    {
//        [JsonPropertyName("status")]
//        public bool Status { get; set; }
//        [JsonPropertyName("data")]
//        public CvStatsData Data { get; set; }
//    }

//    public class ParsedResumeData
//    {
//        [JsonPropertyName("message")]
//        public string? Message { get; set; }
//        [JsonPropertyName("file_key")]
//        public string? FileKey { get; set; }
//        [JsonPropertyName("error")]
//        public string? Error { get; set; }
//        [JsonPropertyName("link")]
//        public string? Link { get; set; }
//        [JsonPropertyName("name")]
//        public string? Name { get; set; }
//        [JsonPropertyName("email")]
//        public string? Email { get; set; }
//        [JsonPropertyName("mobile_number")]
//        public string? Mobile_Number { get; set; }
//        [JsonPropertyName("skills")]
//        public List<string>? Skills { get; set; }
//        [JsonPropertyName("college_name")]
//        public string? College_Name { get; set; }
//        [JsonPropertyName("degree")]
//        public List<string>? Degree { get; set; }
//        [JsonPropertyName("designation")]
//        public List<string>? Designation { get; set; }
//        [JsonPropertyName("experience")]
//        public List<string>? Experience { get; set; }
//        [JsonPropertyName("company_names")]
//        public List<string>? Company_Names { get; set; }
//        [JsonPropertyName("no_of_pages")]
//        public int? No_Of_Pages { get; set; }
//        [JsonPropertyName("total_experience")]
//        public decimal? Total_Experience { get; set; }
//    }

//    public class CvResultsApiResponse
//    {
//        [JsonPropertyName("status")]
//        public bool Status { get; set; }
//        [JsonPropertyName("data")]
//        public Dictionary<string, ParsedResumeData>? Data { get; set; }
//    }
//}