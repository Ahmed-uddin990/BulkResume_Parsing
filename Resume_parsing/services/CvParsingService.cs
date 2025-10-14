using Polly;
using Polly.Retry;
using Resume_parsing.Models;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;

public class CvParsingService
{
    private readonly HttpClient _httpClient;
    private const string ApiCode = "e4b56b5b34fc4c3cbb8f17f8c18fc4c9";

    public CvParsingService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<StartJobApiResponse> StartParsingJobAsync(string userEmail, string folderPath, int fileLimit)
    {
        var formData = new MultipartFormDataContent();
        formData.Add(new StringContent(ApiCode), "code");
        formData.Add(new StringContent("true"), "migrate");
        formData.Add(new StringContent(fileLimit.ToString()), "file_limit");
        formData.Add(new StringContent(userEmail), "user_email");
        formData.Add(new StringContent(folderPath), "folder_path");

        var response = await _httpClient.PostAsync("cv_parser/", formData);
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<StartJobApiResponse>(json);
    }

    public async Task<CvStatsApiResponse> GetJobStatsAsync(string jobIdPython)
    {
        var content = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("uid", jobIdPython),
            new KeyValuePair<string, string>("code", ApiCode)
        });

        AsyncRetryPolicy<HttpResponseMessage> retryPolicy =
            Policy.Handle<HttpRequestException>()
                    .OrResult<HttpResponseMessage>(r => !r.IsSuccessStatusCode)
                    .WaitAndRetryAsync(3, retryAttempt =>
                    {
                        Console.WriteLine($"GetJobStatsAsync retry attempt {retryAttempt} for job {jobIdPython}.");
                        return TimeSpan.FromSeconds(Math.Pow(2, retryAttempt));
                    });

        HttpResponseMessage response = await retryPolicy.ExecuteAsync(() =>
            _httpClient.PostAsync("cv_stats", content)
        );

        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<CvStatsApiResponse>(json);
    }

    public async Task<CvResultsApiResponse> GetJobResultsAsync(string jobIdPython)
    {
        var content = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("uid", jobIdPython),
            new KeyValuePair<string, string>("code", ApiCode)
        });

        HttpResponseMessage response = await _httpClient.PostAsync("cv_results", content);
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<CvResultsApiResponse>(json);
    }

    public async Task<CvApiResponse> ReparseFailedAsync(string jobIdPython)
    {
        var content = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("uid", jobIdPython),
            new KeyValuePair<string, string>("code", ApiCode)
        });

        AsyncRetryPolicy<HttpResponseMessage> retryPolicy =
            Policy.Handle<HttpRequestException>()
                    .OrResult<HttpResponseMessage>(r => !r.IsSuccessStatusCode)
                    .WaitAndRetryAsync(3, retryAttempt =>
                    {
                        Console.WriteLine($"ReparseFailedAsync retry attempt {retryAttempt} for job {jobIdPython}.");
                        return TimeSpan.FromSeconds(Math.Pow(2, retryAttempt));
                    });

        HttpResponseMessage response = await retryPolicy.ExecuteAsync(() =>
            _httpClient.PostAsync("reparse_failed", content)
        );

        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<CvApiResponse>(json);
    }
}
