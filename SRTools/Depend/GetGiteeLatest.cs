// Copyright (c) 2021-2024, JamXi JSG-LLC.
// All rights reserved.

// This file is part of SRTools.

// SRTools is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.

// SRTools is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.

// You should have received a copy of the GNU General Public License
// along with SRTools.  If not, see <http://www.gnu.org/licenses/>.

// For more information, please refer to <https://www.gnu.org/licenses/gpl-3.0.html>


using System;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

public class GetGiteeLatest
{
    private static readonly HttpClient httpClient = new HttpClient();

    public async Task<(string Name, string Version, string DownloadUrl, string Changelog)> GetLatestReleaseInfoAsync(string owner, string repo)
    {
        string apiUrl = $"https://gitee.com/api/v5/repos/{owner}/{repo}/releases/latest";
        httpClient.DefaultRequestHeaders.Add("User-Agent", "C# Gitee API Client");

        var response = await httpClient.GetAsync(apiUrl);
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync();
        JObject jsonObj = JObject.Parse(content);

        var name = jsonObj["name"].ToString();
        var version = jsonObj["tag_name"].ToString();
        var downloadUrl = jsonObj["assets"][0]["browser_download_url"].ToString();
        var Changelog = jsonObj["body"].ToString();

        return (name, version, downloadUrl, Changelog);
    }
}