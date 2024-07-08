using System;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using System.IO;
using System.Diagnostics;
using SRTools.Depend;
using SRTools;

namespace SRTools.Depend
{
    class GetUpdate
    {
        private static readonly GetGithubLatest _getGithubLatest = new GetGithubLatest();
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
                var latestReleaseInfo = await _getJSGLatest.GetLatestReleaseInfoAsync("cn.jamsg.SRTools");
                Logging.Write("Getting Update Info...", 0);

                switch (AppDataController.GetUpdateService())
                {
                    case 0:
                        Logging.Write("UpdateService:Github", 0);
                        if (Mode == "Depend")
                        {
                            latestReleaseInfo = await _getGithubLatest.GetLatestDependReleaseInfoAsync("JamXi233", "Releases", PkgName);
                        }
                        else
                        {
                            latestReleaseInfo = await _getGithubLatest.GetLatestReleaseInfoAsync("JamXi233", PkgName);
                        }
                        break;
                    case 2:
                        Logging.Write("UpdateService:JSG-DS", 0);
                        latestReleaseInfo = await _getJSGLatest.GetLatestReleaseInfoAsync("cn.jamsg." + PkgName);
                        break;
                    default:
                        Logging.Write($"Invalid update service value: {AppDataController.GetUpdateService()}", 0);
                        throw new InvalidOperationException($"Invalid update service value: {AppDataController.GetUpdateService()}");
                }

                Logging.Write("Software Name:" + latestReleaseInfo.Name, 0);
                Logging.Write("Newer Version:" + latestReleaseInfo.Version, 0);

                if (Mode == "Depend")
                {
                    string userDocumentsFolderPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                    string iniPath = Path.Combine(userDocumentsFolderPath, "JSG-LLC", "SRTools", "Depends", PkgName, "SRToolsHelperVersion.ini");
                    string exePath = Path.Combine(userDocumentsFolderPath, "JSG-LLC", "SRTools", "Depends", PkgName, "SRToolsHelper.exe");

                    Version installedVersionParsed;

                    if (File.Exists(iniPath))
                    {
                        string[] iniLines = await File.ReadAllLinesAsync(iniPath);
                        if (iniLines != null)
                        {
                            string versionString = iniLines[0].Trim();
                            installedVersionParsed = new Version(versionString);
                        }
                        else
                        {
                            installedVersionParsed = new Version("0.0.0.0");
                        }
                    }
                    else if (File.Exists(exePath))
                    {
                        FileVersionInfo fileInfo = FileVersionInfo.GetVersionInfo(exePath);
                        installedVersionParsed = new Version(fileInfo.FileVersion);
                    }
                    else
                    {
                        installedVersionParsed = new Version("0.0.0.0");
                    }

                    Version latestVersionParsed = new Version(latestReleaseInfo.Version);
                    if (latestVersionParsed > installedVersionParsed)
                    {
                        App.IsSRToolsHelperRequireUpdate = true;
                        return new UpdateResult(1, latestReleaseInfo.Version, latestReleaseInfo.Changelog);
                    }
                    App.IsSRToolsHelperRequireUpdate = false;
                    return new UpdateResult(0, installedVersionParsed.ToString(), string.Empty);
                }
                else
                {
                    Version latestVersionParsed = new Version(latestReleaseInfo.Version);

                    if (latestVersionParsed > currentVersionParsed)
                    {
                        App.IsSRToolsRequireUpdate = true;
                        return new UpdateResult(1, latestReleaseInfo.Version, latestReleaseInfo.Changelog);
                    }
                    App.IsSRToolsRequireUpdate = false;
                    return new UpdateResult(0, currentVersion, string.Empty);
                }
            }
            catch (Exception)
            {
                return new UpdateResult(2, string.Empty, string.Empty);
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
