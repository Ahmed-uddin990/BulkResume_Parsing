using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Security;
using System.Text.Json.Serialization;

namespace Resume_parsing.Models
{
    // The main job record, linked to the Python API
    // C# Models aligned with the new database schema
    [Table("Jobs")]
    public class Jobs
    {
        [Key]
        public int Id { get; set; }

        [Column("JobIdPython")]
        public Guid JobIdPython { get; set; }

        [Column("Code")]
        public string Code { get; set; }

        [Column("Status")]
        public bool Status { get; set; }

        [Column("JobName")]
        public string JobName { get; set; }

        [Column("OneDriveEmail")]
        public string OneDriveEmail { get; set; }

        [Column("OneDriveFolderPath")]
        public string OneDriveFolderPath { get; set; }

        [Column("File_limit")]
        public string File_limit { get; set; }

        [Column("CreatedBy")]
        public string CreatedBy { get; set; }

        [Column("CreatedDate")]
        public DateTime CreatedDate { get; set; }
        //public int? TotalResumes { get; set; }

        //public int? ProcessedResumes { get; set; }

        //public int? PassedResumes { get; set; }

        //public int? FailedResumes { get; set; }

        //public DateTime? CompletedDate { get; set; }

        public virtual ICollection<JobProgress> JobProgresses { get; set; }
        public virtual ICollection<CV_JobResults> JobResults { get; set; }
    }

    [Table("JobProgress")]
    public class JobProgress
    {
        [Key]
        public int Id { get; set; }

        [Column("Job_Id")]
        public int Job_Id { get; set; }

        [Column("Total")]
        public int? Total { get; set; }

        [Column("Passed")]
        public int? Passed { get; set; }

        [Column("Failed")]
        public int? Failed { get; set; }

        [Column("Processed")]
        public int? Processed { get; set; }

        [Column("CreatedDate")]
        public DateTime CreatedDate { get; set; }

        [ForeignKey("Job_Id")]
        public Jobs Job { get; set; }
    }

    [Table("CV_JobResults")]
    public class CV_JobResults
    {
        [Key]
        public int Id { get; set; }

        public Guid? JobId { get; set; }
        public string? Message { get; set; }
        public bool Status { get; set; }
        public string? FileKey { get; set; }
        public string? Error { get; set; }
        public string? Link { get; set; }
        public string? Name { get; set; }
        public string? Email { get; set; }
        public string? Mobile_Number { get; set; }
        [Column(TypeName = "nvarchar(max)")]
        public string? Skills { get; set; }
        public string? College_Name { get; set; }
        public string? Degree { get; set; }
        public string? Designation { get; set; }
        [Column(TypeName = "nvarchar(max)")]
        public string? Experience { get; set; }
        public string? Company_Names { get; set; }
        public int? No_Of_Pages { get; set; }
        public decimal? Total_Experience { get; set; }
        public DateTime? CreatedDate { get; set; }

        [ForeignKey("JobId")]
        public Jobs Job { get; set; }
    }

    public class StartJobApiResponse
    {
        [JsonPropertyName("status")]
        public bool Status { get; set; }
        [JsonPropertyName("data")]
        public string Data { get; set; }
    }

    public class CvStatsData
    {
        [JsonPropertyName("total")]
        public int Total { get; set; }
        [JsonPropertyName("passed")]
        public int Passed { get; set; }
        [JsonPropertyName("failed")]
        public int Failed { get; set; }
        [JsonPropertyName("processed")]
        public int Processed { get; set; }
        [JsonPropertyName("failed_names")]
        public List<string> FailedNames { get; set; }
    }

    public class CvStatsApiResponse
    {
        [JsonPropertyName("status")]
        public bool Status { get; set; }
        [JsonPropertyName("data")]
        public CvStatsData Data { get; set; }
    }

    public class ParsedResumeData
    {
        [JsonPropertyName("message")]
        public string? Message { get; set; }
        [JsonPropertyName("file_key")]
        public string? FileKey { get; set; }
        [JsonPropertyName("error")]
        public string? Error { get; set; }
        [JsonPropertyName("link")]
        public string? Link { get; set; }
        [JsonPropertyName("name")]
        public string? Name { get; set; }
        [JsonPropertyName("email")]
        public string? Email { get; set; }
        [JsonPropertyName("mobile_number")]
        public string? Mobile_Number { get; set; }
        [JsonPropertyName("skills")]
        public List<string>? Skills { get; set; }
        [JsonPropertyName("college_name")]
        public string? College_Name { get; set; }
        [JsonPropertyName("degree")]
        public List<string>? Degree { get; set; }
        [JsonPropertyName("designation")]
        public List<string>? Designation { get; set; }
        [JsonPropertyName("experience")]
        public List<string>? Experience { get; set; }
        [JsonPropertyName("company_names")]
        public List<string>? Company_Names { get; set; }
        [JsonPropertyName("no_of_pages")]
        public int? No_Of_Pages { get; set; }
        [JsonPropertyName("total_experience")]
        public decimal? Total_Experience { get; set; }
    }

    public class CvResultsApiResponse
    {
        [JsonPropertyName("status")]
        public bool Status { get; set; }
        [JsonPropertyName("data")]
        public Dictionary<string, ParsedResumeData>? Data { get; set; }
    }
    public class JobProgressViewModel
    {
        public int JobIdDatabase { get; set; }
        public Guid JobIdPython { get; set; }
        public string JobName { get; set; }
        public int? Total { get; set; }
        public int? Processed { get; set; }
        public int? Passed { get; set; }
        public int? Failed { get; set; }
        public DateTime CreatedDate { get; set; }
    }

    // Models/CvApiResponse.cs
  
        public class CvApiResponse
        {
            public bool Status { get; set; }
            public string Message { get; set; }
            public string Data { get; set; }
        }

    //public class FtpSettings
    //{
    //    public string Host { get; set; }
    //    public string Ftp { get; set; }
    //    public string FtpImgFolder { get; set; }
    //    public string FtpResumeFolder { get; set; }
    //    public string FtpUserName { get; set; }
    //    public string FtpPassword { get; set; }
    //    public string Username { get; set; }
    //    public string Password { get; set; }
    //    public string ResumeFolder { get; set; }
    //}

    public class FtpSettings
    {
        public string Host { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string ResumeFolder { get; set; }
    }
    public class JobStatusRequest
    {
        public int JobDatabaseId { get; set; }
        public string JobIdPython { get; set; }
    }
}


