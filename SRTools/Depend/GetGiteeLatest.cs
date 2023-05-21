// GetGiteeLatest.cs

using System;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

public class GetGiteeLatest
{
    private static readonly HttpClient httpClient = new HttpClient();

    public async Task<(string Version, string DownloadUrl)> GetLatestReleaseInfoAsync(string owner, string repo)
    {
        string apiUrl = $"https://gitee.com/api/v5/repos/{owner}/{repo}/releases/latest";
        httpClient.DefaultRequestHeaders.Add("User-Agent", "C# Gitee API Client");

        var response = await httpClient.GetAsync(apiUrl);
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync();
        JObject jsonObj = JObject.Parse(content);


        var version = jsonObj["tag_name"].ToString();
        var downloadUrl = jsonObj["assets"][0]["browser_download_url"].ToString();

        return (version, downloadUrl);
    }
}