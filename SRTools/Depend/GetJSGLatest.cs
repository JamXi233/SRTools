// GetJSGLatest.cs

using System;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

public class GetJSGLatest
{
    private static readonly HttpClient httpClient = new HttpClient();

    public async Task<(string Name, string Version, string DownloadUrl)> GetLatestReleaseInfoAsync(string package)
    {
        string apiUrl = $"https://api.jamsg.cn/release/getversion.php?package={package}";
        httpClient.DefaultRequestHeaders.Add("User-Agent", "JSG-Official-Update-Client");

        var response = await httpClient.GetAsync(apiUrl);
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync();
        JObject jsonObj = JObject.Parse(content);

        var name = jsonObj["name"].ToString();
        var version = jsonObj["version"].ToString();
        var downloadUrl = jsonObj["link"].ToString();

        return (name, version, downloadUrl);
    }
}