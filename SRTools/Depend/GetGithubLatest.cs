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

using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace SRTools.Depend
{
    internal class GetGithubLatest
    {
        private static readonly HttpClient httpClient = new HttpClient();

        public async Task<(string Name, string Version, string DownloadUrl, string Changelog)> GetLatestReleaseInfoAsync(string owner, string repo)
        {
            string apiUrl = $"https://api.github.com/repos/{owner}/{repo}/releases/latest";
            httpClient.DefaultRequestHeaders.Add("User-Agent", "SRTools-Update-Client");

            try
            {
                var response = await httpClient.GetAsync(apiUrl);
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                JObject jsonObj = JObject.Parse(content);

                var name = jsonObj["name"].ToString();
                var version = jsonObj["tag_name"].ToString();
                var changelog = jsonObj["body"].ToString();
                var downloadUrl = jsonObj["assets"][0]["browser_download_url"].ToString();

                Logging.Write($"Fetched latest release info: Name={name}, Version={version}, DownloadUrl={downloadUrl}");
                return (name, version, downloadUrl, changelog);
            }
            catch (Exception ex)
            {
                Logging.Write($"Error fetching latest release info: {ex.Message}",2);
                throw;
            }
        }

        public async Task<(string Name, string Version, string DownloadUrl, string Changelog)> GetLatestDependReleaseInfoAsync(string owner, string repo, string assetPrefix)
        {
            string apiUrl = $"https://api.github.com/repos/{owner}/{repo}/releases";
            httpClient.DefaultRequestHeaders.Add("User-Agent", "SRTools-Update-Client");

            try
            {
                var response = await httpClient.GetAsync(apiUrl);
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                JArray jsonArray = JArray.Parse(content);

                foreach (var release in jsonArray)
                {
                    foreach (var asset in release["assets"])
                    {
                        if (asset["name"].ToString().StartsWith(assetPrefix))
                        {
                            var version = release["tag_name"].ToString();
                            if (version.StartsWith(assetPrefix))
                            {
                                version = version.Substring(assetPrefix.Length).TrimStart('_');
                            }
                            var name = assetPrefix;
                            var changelog = release["body"].ToString();
                            var downloadUrl = asset["browser_download_url"].ToString();

                            Console.WriteLine($"Found matching asset: Name={name}, Version={version}, DownloadUrl={downloadUrl}");
                            return (name, version, downloadUrl, changelog);
                        }
                    }
                }

                throw new Exception($"No assets found with prefix {assetPrefix}");
            }
            catch (Exception ex)
            {
                Logging.Write($"Error fetching release info for prefix {assetPrefix}: {ex.Message}",2);
                throw;
            }
        }
    }
}
