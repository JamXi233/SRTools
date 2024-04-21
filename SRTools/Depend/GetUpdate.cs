using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.Storage;
using System.IO;

namespace SRTools.Depend
{
    class GetUpdate
    {
        private static readonly GetGiteeLatest _getGiteeLatest = new GetGiteeLatest();
        private static readonly GetJSGLatest _getJSGLatest = new GetJSGLatest();

        public static async Task<UpdateResult> GetSRToolsUpdate()
        {
            UpdateResult result = await OnGetUpdateLatestReleaseInfo("SRTools");
            return result;
        }

        public static async Task<UpdateResult> GetDependUpdate()
        {
            UpdateResult result = await OnGetUpdateLatestReleaseInfo("SRToolsHelper", "Depend");
            return result;
        }

        private static async Task<UpdateResult> OnGetUpdateLatestReleaseInfo(string PkgName, string Mode = null)
        {
            PackageVersion packageVersion = Package.Current.Id.Version;
            string currentVersion = $"{packageVersion.Major}.{packageVersion.Minor}.{packageVersion.Build}.{packageVersion.Revision}";
            Version currentVersionParsed = new Version(currentVersion);
            try
            {
                ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
                var latestReleaseInfo = await _getJSGLatest.GetLatestReleaseInfoAsync("cn.jamsg.SRTools");
                Logging.Write("Getting Update Info...", 0);

                switch (AppDataController.GetUpdateService())
                {
                    case 0:
                        Logging.Write("UService:Github", 0);
                        break;
                    case 1:
                        Logging.Write("UService:Gitee", 0);
                        latestReleaseInfo = await _getGiteeLatest.GetLatestReleaseInfoAsync("JSG-JamXi", PkgName);
                        break;
                    case 2:
                        Logging.Write("UService:JSG-DS", 0);
                        latestReleaseInfo = await _getJSGLatest.GetLatestReleaseInfoAsync("cn.jamsg." + PkgName);
                        break;
                    default:
                        Logging.Write($"Invalid update service value: {localSettings.Values["Config_UpdateService"]}", 0);
                        throw new InvalidOperationException($"Invalid update service value: {localSettings.Values["Config_UpdateService"]}");
                }

                Logging.Write("Software Name:" + latestReleaseInfo.Name, 0);
                Logging.Write("Newer Version:" + latestReleaseInfo.Version, 0);

                // Handle Depend Mode differently
                if (Mode == "Depend")
                {
                    string userDocumentsFolderPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                    string path = Path.Combine(userDocumentsFolderPath, "JSG-LLC", "SRTools", "Depends", PkgName, "SRToolsHelperVersion.ini");
                    string content = File.Exists(path) ? File.ReadAllText(path) : "0";
                    Version installedVersionParsed = new Version(content);

                    if (new Version(latestReleaseInfo.Version) > installedVersionParsed)
                    {
                        return new UpdateResult(1, latestReleaseInfo.Version, latestReleaseInfo.Changelog);
                    }
                    return new UpdateResult(0, content, string.Empty); // No update required
                }
                else
                {
                    Version latestVersionParsed = new Version(latestReleaseInfo.Version);

                    if (latestVersionParsed > currentVersionParsed)
                    {
                        return new UpdateResult(1, latestReleaseInfo.Version, latestReleaseInfo.Changelog);
                    }
                    return new UpdateResult(0, currentVersion, string.Empty); // No update required
                }
            }
            catch (Exception)
            {
                return new UpdateResult(2, string.Empty, string.Empty); // Error state
            }
        }

    }
    public class UpdateResult
    {
        public int Status { get; set; } // 0: 无更新, 1: 有更新, 2: 错误
        public string Version { get; set; } // 版本号
        public string Changelog { get; set; } // 更新日志

        public UpdateResult(int status, string version, string changelog)
        {
            Status = status;
            Version = version;
            Changelog = changelog;

        }
    }

}
