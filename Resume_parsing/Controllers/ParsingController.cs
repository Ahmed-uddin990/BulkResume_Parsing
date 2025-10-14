using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Resume_parsing.Data;
using Resume_parsing.Models;
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Concurrent;

[ApiController]
[Route("api/[controller]")]
public class ParsingController : Controller
{
    private readonly CvParsingService _cvParsingService;
    private readonly IServiceProvider _serviceProvider;

    // We keep the main DbContext for methods that are part of the request's scope.
    private readonly ApplicationDbContext _dbContext;

    public ParsingController(CvParsingService cvParsingService, ApplicationDbContext dbContext, IServiceProvider serviceProvider)
    {
        _cvParsingService = cvParsingService;
        _dbContext = dbContext;
        _serviceProvider = serviceProvider;
    }

    [HttpGet("/")]
    public IActionResult Index()
    {
        return View();
    }

    [HttpPost("start")]
    public async Task<IActionResult> StartJob(
        [FromForm] string jobName,
        [FromForm] string userEmail,
        [FromForm] string folderPath,
        [FromForm] int fileLimit)
    {
        try
        {
            var apiResponse = await _cvParsingService.StartParsingJobAsync(userEmail, folderPath, fileLimit);

            if (!apiResponse.Status || string.IsNullOrEmpty(apiResponse.Data))
            {
                return BadRequest(new { message = "Failed to start the Python job. The external API returned an error." });
            }

            var newJob = new Jobs
            {
                JobIdPython = Guid.Parse(apiResponse.Data),
                Code = "Running",
                Status = true,
                JobName = jobName,
                OneDriveEmail = userEmail,
                OneDriveFolderPath = folderPath,
                File_limit = fileLimit.ToString(),
                CreatedBy = "Admin",
                CreatedDate = DateTime.Now
            };

            _dbContext.Jobs.Add(newJob);
            await _dbContext.SaveChangesAsync();

            return Ok(new { jobDatabaseId = newJob.Id, jobIdPython = newJob.JobIdPython.ToString() });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"StartJob error: {ex.Message} \n {ex.StackTrace}");
            return StatusCode(500, new { message = "An error occurred: " + ex.Message });
        }
    }

    [HttpPost("status")]
    public async Task<IActionResult> CheckJobStatus([FromBody] JobStatusRequest request)
    {
        try
        {
            var jobRecord = await _dbContext.Jobs.FirstOrDefaultAsync(j => j.Id == request.JobDatabaseId);
            if (jobRecord == null)
            {
                return NotFound(new { message = "Job not found." });
            }

            // If the job is already marked as completed, return the final data immediately.
            if (jobRecord.Code == "Completed")
            {
                var finalProgress = await _dbContext.JobProgresses.FirstOrDefaultAsync(jp => jp.Job_Id == request.JobDatabaseId);
                return Ok(new { data = finalProgress, isComplete = true, failedCount = finalProgress?.Failed ?? 0 });
            }

            var statsResponse = await _cvParsingService.GetJobStatsAsync(request.JobIdPython);

            if (!statsResponse.Status || statsResponse.Data == null)
            {
                // This indicates a temporary API issue, we return the current status from our DB.
                var currentProgress = await _dbContext.JobProgresses.FirstOrDefaultAsync(jp => jp.Job_Id == request.JobDatabaseId);
                return Ok(new { data = currentProgress, isComplete = false, failedCount = currentProgress?.Failed ?? 0 });
            }

            bool isApiComplete = statsResponse.Data.Total > 0 && statsResponse.Data.Processed >= statsResponse.Data.Total;

            if (isApiComplete)
            {
                // Offload the heavy work to a background task that uses its own DbContext scope.
                // The client receives an immediate response.
                _ = Task.Run(async () => await FinalizeJobAsync(request.JobDatabaseId, request.JobIdPython));
                return Ok(new { data = statsResponse.Data, isComplete = true, failedCount = statsResponse.Data.Failed });
            }
            else
            {
                // Update progress and return the current status.
                await UpdateJobProgress(request.JobDatabaseId, statsResponse.Data);
                return Ok(new { data = statsResponse.Data, isComplete = false, failedCount = statsResponse.Data.Failed });
            }
        }     
        catch (Exception ex)
        {
            Console.WriteLine($"CheckJobStatus error: {ex.Message} \n {ex.StackTrace}");
            return StatusCode(500, new { message = "An error occurred: " + ex.Message });
        }
    }

    [HttpPost("reparse-failed")]
    public async Task<IActionResult> ReparseFailed([FromForm] string jobIdPython)
    {
        try
        {
            var apiResponse = await _cvParsingService.ReparseFailedAsync(jobIdPython);

            if (!apiResponse.Status)
            {
                return BadRequest(new { message = apiResponse.Message ?? "Failed to start re-parsing job." });
            }

            // Offload the database update and cleanup to a background task
            _ = Task.Run(async () =>
            {
                using var scope = _serviceProvider.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                var jobRecord = await dbContext.Jobs.FirstOrDefaultAsync(j => j.JobIdPython.ToString() == jobIdPython);
                if (jobRecord != null)
                {
                    jobRecord.Code = "Running";
                    jobRecord.Status = true;
                    await dbContext.SaveChangesAsync();
                }

                var resultsToDelete = await dbContext.JobResults.Where(jr => jr.JobId.ToString() == jobIdPython).ToListAsync();
                dbContext.JobResults.RemoveRange(resultsToDelete);
                await dbContext.SaveChangesAsync();
            });

            return Ok(new { message = "Re-parsing job started successfully." });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ReparseFailed error: {ex.Message} \n {ex.StackTrace}");
            return StatusCode(500, new { message = "An error occurred: " + ex.Message });
        }
    }

    private async Task UpdateJobProgress(int jobDatabaseId, CvStatsData statsData)
    {
        // This method can use the existing DbContext because it's part of the request scope
        var jobProgress = await _dbContext.JobProgresses.FirstOrDefaultAsync(jp => jp.Job_Id == jobDatabaseId);
        if (jobProgress == null)
        {
            jobProgress = new JobProgress
            {
                Job_Id = jobDatabaseId,
                CreatedDate = DateTime.Now
            };
            _dbContext.JobProgresses.Add(jobProgress);
        }

        jobProgress.Total = statsData.Total;
        jobProgress.Processed = statsData.Processed;
        jobProgress.Passed = statsData.Passed;
        jobProgress.Failed = statsData.Failed;

        await _dbContext.SaveChangesAsync();
    }

    private async Task FinalizeJobAsync(int jobDatabaseId, string jobIdPython)
    {
        // Use a new DbContext scope for this background task
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        try
        {
            var jobRecord = await dbContext.Jobs.FirstOrDefaultAsync(j => j.Id == jobDatabaseId);
            if (jobRecord == null || jobRecord.Code == "Completed")
            {
                return;
            }

            var resultsResponse = await _cvParsingService.GetJobResultsAsync(jobIdPython);
            if (resultsResponse.Status && resultsResponse.Data != null)
            {
                foreach (var fileEntry in resultsResponse.Data)
                {
                    var fileData = fileEntry.Value;
                    var record = new CV_JobResults
                    {
                        JobId = Guid.Parse(jobIdPython),
                        Message = fileData.Message,
                        Status = fileData.Error == null,
                        FileKey = fileEntry.Key,
                        Error = fileData.Error,
                        Link = fileData.Link,
                        Name = fileData.Name,
                        Email = fileData.Email,
                        Mobile_Number = fileData.Mobile_Number,
                        Skills = fileData.Skills != null ? string.Join(",", fileData.Skills) : null,
                        Degree = fileData.Degree != null ? string.Join(",", fileData.Degree) : null,
                        Designation = fileData.Designation != null ? string.Join(",", fileData.Designation) : null,
                        Experience = fileData.Experience != null ? string.Join("\n", fileData.Experience) : null,
                        Company_Names = fileData.Company_Names != null ? string.Join(",", fileData.Company_Names) : null,
                        College_Name = fileData.College_Name,
                        No_Of_Pages = fileData.No_Of_Pages,
                        Total_Experience = fileData.Total_Experience,
                        CreatedDate = DateTime.Now
                    };
                    dbContext.JobResults.Add(record);
                }
                await dbContext.SaveChangesAsync();
            }

            jobRecord.Code = "Completed";
            jobRecord.Status = false;
            await dbContext.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in FinalizeJobAsync: {ex.Message} \n {ex.StackTrace}");
        }
    }
}