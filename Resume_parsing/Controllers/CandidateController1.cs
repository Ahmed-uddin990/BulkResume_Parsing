using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Resume_parsing.Data;
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using System.Net;
using System.IO;

namespace Resume_parsing.Models
{
    public class CandidateController : Controller
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly FtpSettings _ftpSettings;

        // Constructor that receives the FtpSettings via dependency injection
        public CandidateController(ApplicationDbContext context, IOptions<FtpSettings> ftpSettings)
        {
            _dbContext = context;
            _ftpSettings = ftpSettings.Value;
        }

        // This action queries the database to build the candidate grid.
        public IActionResult Index(string skillFilter, string minExperience, string maxExperience)
        {
            try
            {
                var query = _dbContext.JobResults.AsQueryable();

                if (!string.IsNullOrEmpty(skillFilter))
                {
                    query = query.Where(c => c.Skills != null && c.Skills.Contains(skillFilter));
                }

                if (!string.IsNullOrEmpty(minExperience) && decimal.TryParse(minExperience, out decimal parsedMinExperience))
                {
                    query = query.Where(c => c.Total_Experience >= parsedMinExperience);
                }

                if (!string.IsNullOrEmpty(maxExperience) && decimal.TryParse(maxExperience, out decimal parsedMaxExperience))
                {
                    query = query.Where(c => c.Total_Experience <= parsedMaxExperience);
                }

                var rawResults = query
                    .Select(c => new
                    {
                        Name = c.Name ?? "N/A",
                        Skills = c.Skills ?? "N/A",
                        TotalExperience = c.Total_Experience,
                        // This 'Link' property now corresponds to the FTP filename
                        Link = c.Link ?? "#"
                    })
                    .ToList();

                var processedResults = rawResults.Select(c =>
                {
                    string skillsForDisplay = c.Skills;

                    if (!string.IsNullOrEmpty(skillFilter) && skillsForDisplay != null)
                    {
                        var skillsLower = skillsForDisplay.ToLower();
                        var filterLower = skillFilter.ToLower();
                        int index = skillsLower.IndexOf(filterLower);
                        if (index != -1)
                        {
                            string before = skillsForDisplay.Substring(0, index);
                            string match = skillsForDisplay.Substring(index, filterLower.Length);
                            string after = skillsForDisplay.Substring(index + filterLower.Length);
                            skillsForDisplay = $"{before}<span class='highlight'>{match}</span>{after}";
                        }
                    }

                    if (skillsForDisplay != null)
                    {
                        var allSkills = skillsForDisplay.Split(',').Select(s => s.Trim()).ToList();
                        if (allSkills.Count > 4)
                        {
                            skillsForDisplay = string.Join(", ", allSkills.Take(4)) + "...";
                        }
                    }

                    return new
                    {
                        c.Name,
                        Skills = skillsForDisplay,
                        c.TotalExperience,
                        c.Link
                    };
                }).ToList();

                ViewData["skillFilter"] = skillFilter;
                ViewData["minExperience"] = minExperience;
                ViewData["maxExperience"] = maxExperience;

                return View(processedResults);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                ViewBag.ErrorMessage = "Something went wrong while loading candidates.";
                return View("Error");
            }
        }

        // This is a placeholder for the JobProgressHistory action from the original code.
        public async Task<IActionResult> JobProgressHistory()
        {
            try
            {
                var jobProgresses = await _dbContext.JobProgresses
                  .Include(jp => jp.Job)
                  .OrderByDescending(jp => jp.CreatedDate)
                  .Select(jp => new
                  {
                      JobIdPython = jp.Job.JobIdPython,
                      JobName = jp.Job.JobName,
                      Total = jp.Total,
                      Processed = jp.Processed,
                      Passed = jp.Passed,
                      Failed = jp.Failed,
                      CreatedDate = jp.CreatedDate
                  })
                  .ToListAsync();
                return View(jobProgresses);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                ViewBag.ErrorMessage = "Something went wrong while loading job progress history.";
                return View("Error");
            }
        }

        // Refactored and consolidated DownloadCv action for downloading resumes from FTP
        public async Task<IActionResult> DownloadCv1(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
            {
                TempData["Success"] = false;
                TempData["shortMessage"] = "filename not present";
                return RedirectToAction("Index");
            }

            // Construct the full FTP path using the configured settings.
            string fullFtpPath = $"ftp://{_ftpSettings.Host}{_ftpSettings.ResumeFolder}/{fileName}";

            try
            {
                // Create the FTP request
                var ftpRequest = (FtpWebRequest)WebRequest.Create(fullFtpPath);

                // Configure request properties
                ftpRequest.Credentials = new NetworkCredential(_ftpSettings.Username, _ftpSettings.Password);
                ftpRequest.UseBinary = true;
                ftpRequest.UsePassive = true;
                ftpRequest.KeepAlive = true;
                ftpRequest.Method = WebRequestMethods.Ftp.DownloadFile;

                var ftpResponse = (FtpWebResponse)await ftpRequest.GetResponseAsync();
                var ftpStream = ftpResponse.GetResponseStream();

                // Determine content type based on file extension
                var fileExt = Path.GetExtension(fileName).ToLowerInvariant();
                string contentType;
                switch (fileExt)
                {
                    case ".pdf":
                        contentType = "application/pdf";
                        break;
                    case ".doc":
                    case ".docx":
                        contentType = "application/vnd.openxmlformats-officedocument.wordprocessingml.document";
                        break;
                    case ".txt":
                        contentType = "text/plain";
                        break;
                    default:
                        contentType = "application/octet-stream";
                        break;
                }

                // Return the file stream. The File method will automatically close the stream.
                return File(ftpStream, contentType, fileName);
            }
            catch (WebException ex)
            {
                Console.WriteLine($"FTP download failed for file {fileName}: {ex.Message}");
                TempData["Success"] = false;
                TempData["shortMessage"] = "The requested file was not found on the server or an FTP error occurred.";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                TempData["Success"] = false;
                TempData["shortMessage"] = "An unexpected error occurred.";
                Console.WriteLine($"An unexpected error occurred: {ex.Message}");
                return RedirectToAction("Index");
            }
        }

        [HttpGet]
        public ActionResult DownloadCv(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
            {
                TempData["shortMessage"] = "Invalid or missing filename.";
                TempData["Success"] = false;
                return RedirectToAction("Index");
            }

            try
            {
                // Build FTP URI
                string host = _ftpSettings.Host;
                if (!host.EndsWith("/"))
                    host += "/";

                string folder = _ftpSettings.ResumeFolder.TrimStart('/').TrimEnd('/');
                string ftpPath = folder + "/" + fileName;
                Uri ftpUri = new Uri(host + ftpPath);

                // Create FTP request
                var request = (FtpWebRequest)WebRequest.Create(ftpUri);
                request.Credentials = new NetworkCredential(_ftpSettings.Username, _ftpSettings.Password);
                request.Method = WebRequestMethods.Ftp.DownloadFile;
                request.UseBinary = true;
                request.UsePassive = true;
                request.KeepAlive = false;

                // Get FTP response and stream
                using (var response = (FtpWebResponse)request.GetResponse())
                using (var responseStream = response.GetResponseStream())
                {
                    if (responseStream == null)
                    {
                        TempData["shortMessage"] = "Failed to read file from FTP.";
                        TempData["Success"] = false;
                        return RedirectToAction("Index");
                    }

                    var contentType = GetContentType(fileName);

                    // Return file to client
                    return File(responseStream, contentType, fileName);
                }
            }
            catch (WebException ex)
            {
                var response = ex.Response as FtpWebResponse;
                TempData["shortMessage"] = response != null
                    ? $"FTP Error: {response.StatusDescription}"
                    : $"Network error: {ex.Message}";
                TempData["Success"] = false;
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                TempData["shortMessage"] = "An unexpected error occurred.";
                TempData["Success"] = false;
                return RedirectToAction("Index");
            }
        }

        private string GetContentType(string fileName)
        {
            var ext = Path.GetExtension(fileName).ToLowerInvariant();
            return ext switch
            {
                ".pdf" => "application/pdf",
                ".doc" => "application/msword",
                ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                ".txt" => "text/plain",
                ".rtf" => "application/rtf",
                ".xls" => "application/vnd.ms-excel",
                ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                ".csv" => "text/csv",
                _ => "application/octet-stream"
            };
        }
    }




}
