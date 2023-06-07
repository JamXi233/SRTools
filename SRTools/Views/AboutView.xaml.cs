using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.Win32;
using SRTools.Depend;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.System;
using System.IO.Compression;
using Org.BouncyCastle.Asn1.X509;
using Windows.Storage.Pickers;
using Vanara.PInvoke;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace SRTools.Views
{
    public sealed partial class AboutView : Page
    {
        string fileUrl;
        private GetNetData _getNetData;
        private readonly GetGiteeLatest _getGiteeLatest = new GetGiteeLatest();
        private readonly GetJSGLatest _getJSGLatest = new GetJSGLatest();
        // 导入 AllocConsole 和 FreeConsole 函数
        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool AllocConsole();

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool FreeConsole();
        private const int STD_OUTPUT_HANDLE = -11;

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr GetStdHandle(int nStdHandle);

        [DllImport("kernel32.dll")]
        private static extern IntPtr GetConsoleWindow();

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        public AboutView()
        {
            //Windows.ApplicationModel.Core.CoreApplication.UnhandledErrorDetected += OnUnhandledErrorDetected;
            InitializeComponent();
            Logging.Write("Switch to AboutView", 0);
            ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
            switch (localSettings.Values["Config_UpdateService"])
            {
                case 0:
                    uservice_Github.IsChecked = true;
                    break;
                case 1:
                    uservice_Gitee.IsChecked = true;
                    break;
                case 2:
                    uservice_JSG.IsChecked = true;
                    break;
                default:
                    throw new InvalidOperationException($"Invalid update service value: {localSettings.Values["Config_UpdateService"]}");
            }
            switch (localSettings.Values["Config_TerminalMode"])
            {
                case 0:
                    consoleToggle.IsChecked = false;
                    break;
                case 1:
                    consoleToggle.IsChecked = true;
                    break;
                default:
                    consoleToggle.IsChecked = false;
                    break;
            }
            bool isDebug = false;
            #if DEBUG
            isDebug = true;
            #else
            #endif
            if (isDebug)
            {
                consoleToggle.IsEnabled = false;
                debug_Mode.Visibility = Visibility.Visible;
            }
        }
        private void Console_Toggle(object sender, RoutedEventArgs e)
        {
            var currentProcess = Process.GetCurrentProcess();
            var hWnd = currentProcess.MainWindowHandle;
            ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
            // 判断是否需要打开控制台
            if (consoleToggle.IsChecked ?? false)
            {
                TerminalTip.IsOpen = true;
                TerminalMode.ShowConsole();
                localSettings.Values["Config_TerminalMode"] = 1;
                Logging.Write("Console Enabled",1);
            }
            else
            {
                TerminalMode.HideConsole();
                localSettings.Values["Config_TerminalMode"] = 0;
            }
        }

        public void Clear_AllData(object sender, RoutedEventArgs e)
        {
            string userDocumentsFolderPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            DeleteFolder(userDocumentsFolderPath + "\\JSG-LLC\\SRTools\\", "1");
        }

        private void Clear_AllData_NoClose(object sender, RoutedEventArgs e, string Close = "0")
        {
            string userDocumentsFolderPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            DeleteFolder(userDocumentsFolderPath + "\\JSG-LLC\\SRTools\\", Close);
        }

        private void DeleteFolder(string folderPath, String Close)
        {
            if (Directory.Exists(folderPath))
            {
                try { Directory.Delete(folderPath, true); }
                catch (IOException) { }
            }
            _ = ClearLocalDataAsync(Close);
        }

        public async Task ClearLocalDataAsync(String Close)
        {
            // 获取 LocalData 文件夹的引用
            var localFolder = ApplicationData.Current.LocalFolder;

            // 删除 LocalData 文件夹中的所有子文件夹和文件
            await DeleteFilesAndSubfoldersAsync(localFolder, Close);

            // 需要重新创建删除的 LocalData 文件夹
            await ApplicationData.Current.ClearAsync(ApplicationDataLocality.Local);
        }

        private async Task DeleteFilesAndSubfoldersAsync(StorageFolder folder, String Close)
        {
            // 获取文件夹中的所有文件和子文件夹
            var items = await folder.GetItemsAsync();

            // 遍历所有项目
            foreach (var item in items)
            {
                // 如果项目是文件，则删除它
                if (item is StorageFile file)
                {
                    await file.DeleteAsync();
                }
                // 如果项目是文件夹，则递归删除其中所有文件和子文件夹
                else if (item is StorageFolder subfolder)
                {
                    await DeleteFilesAndSubfoldersAsync(subfolder, Close);

                    // 删除子文件夹本身
                    await subfolder.DeleteAsync();
                }
            }
            if (Close == "1")
            {
                Application.Current.Exit();
            }
        }
        private async void Check_Update(object sender, RoutedEventArgs e)
        {
            int result = await OnGetUpdateLatestReleaseInfo("SRTools");
            if (result == 0)
            {
                UpdateTip.IsOpen = true;
                UpdateTip.Subtitle = "无可用更新";
                checkUpdate.IsEnabled = false;
            }
            else if (result == 1)
            {
                Update.Visibility = Visibility.Visible;
                MainAPP.Visibility = Visibility.Collapsed;
                update_Download.Visibility = Visibility.Visible;
                depend_Download.Visibility = Visibility.Collapsed;
            }
            else
            {
                UpdateTip.IsOpen = true;
                UpdateTip.Subtitle = "网络连接失败，可能是请求次数过多";
            }
        }

        private async void Check_Depend_Update(object sender, RoutedEventArgs e)
        {
            int result = await OnGetUpdateLatestReleaseInfo("SRToolsHelper", "Depend");
            if (result == 0)
            {
                DependUpdateTip.IsOpen = true;
                DependUpdateTip.Subtitle = "无可用更新";
                checkDependUpdate.IsEnabled = false;
            }
            else if (result == 1)
            {
                Update.Visibility = Visibility.Visible;
                MainAPP.Visibility = Visibility.Collapsed;
                update_Download.Visibility = Visibility.Collapsed;
                depend_Download.Visibility = Visibility.Visible;
            }
        }

        //选择下载渠道开始
        private void uservice_Github_Choose(object sender, RoutedEventArgs e)
        {
            ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
            localSettings.Values["Config_UpdateService"] = 0;
        }

        private void uservice_Gitee_Choose(object sender, RoutedEventArgs e)
        {
            ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
            localSettings.Values["Config_UpdateService"] = 1;
        }

        private void uservice_JSG_Choose(object sender, RoutedEventArgs e)
        {
            ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
            localSettings.Values["Config_UpdateService"] = 2;
        }

        //更新开始
        public async Task<int> OnGetUpdateLatestReleaseInfo(String PkgName, String Mode = null)
        {
            PackageVersion packageVersion = Package.Current.Id.Version;
            string version = $"{packageVersion.Major}.{packageVersion.Minor}.{packageVersion.Build}.{packageVersion.Revision}";
            Logging.Write("Current Version:" + version, 0);
            update_Grid.Visibility = Visibility.Collapsed;
            update_Progress_Grid.Visibility = Visibility.Visible;
            var dispatcher = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();
            try
            {
                ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
                var latestReleaseInfo = await _getJSGLatest.GetLatestReleaseInfoAsync("cn.jamsg.SRTools");
                Logging.Write("Getting Update Info...", 0);
                switch (localSettings.Values["Config_UpdateService"])
                {
                    case 0:
                        Logging.Write("UService:Github", 0);
                        //latestReleaseInfo = await _getGithubLatest.GetLatestReleaseInfoAsync("JamXi233", "SRToolsHelper");
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
                if (Mode == "Depend")
                {
                    string userDocumentsFolderPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                    string path = userDocumentsFolderPath + "\\JSG-LLC\\SRTools\\Depends\\SRToolsHelper\\SRToolsHelperVersion.ini";
                    try { string content = File.ReadAllText(path); }
                    catch
                    {
                        string content = "Null";
                        if (latestReleaseInfo.Version != content)
                        {
                            fileUrl = latestReleaseInfo.DownloadUrl;
                            update_Latest_Name.Text = $"软件名称: {latestReleaseInfo.Name}";
                            update_Latest_Version.Text = $"版本号: {latestReleaseInfo.Version}";
                            update_Current_Version.Text = $"当前版本: {content}";
                            update_Latest_Changelog.Text = $"更新日志: \n{latestReleaseInfo.Changelog}";
                            update_Download.IsEnabled = true;
                            depend_Grid.Visibility = Visibility.Visible;
                            depend_Progress_Grid.Visibility = Visibility.Collapsed;
                            return 1;
                        }
                        else
                        {
                            return 0;
                        }
                    }
                    return 0;
                }
                else
                {
                    if (latestReleaseInfo.Version != version)
                    {
                        fileUrl = latestReleaseInfo.DownloadUrl;
                        update_Latest_Name.Text = $"软件名称: {latestReleaseInfo.Name}";
                        update_Latest_Version.Text = $"版本号: {latestReleaseInfo.Version}";
                        update_Current_Version.Text = $"当前版本: {version}";
                        update_Latest_Changelog.Text = $"更新日志: \n{latestReleaseInfo.Changelog}";
                        update_Download.IsEnabled = true;
                        update_Grid.Visibility = Visibility.Visible;
                        update_Progress_Grid.Visibility = Visibility.Collapsed;
                        return 1;
                    }
                    else
                    {
                        return 0;
                    }
                }

            }
            catch (Exception ex)
            {
                ContentDialog dialog = new ContentDialog();
                dialog.XamlRoot = this.XamlRoot;
                dialog.Style = Application.Current.Resources["DefaultContentDialogStyle"] as Style;
                dialog.Title = "检查更新出现问题";
                dialog.Content = ex.Message;
                dialog.PrimaryButtonText = "确定";
                dialog.DefaultButton = ContentDialogButton.Primary;
                var result = await dialog.ShowAsync();
                return 2;
            }
        }

        private async void UpdateDownload_Click(object sender, RoutedEventArgs e)
        {
            _getNetData = new GetNetData();
            ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
            var latestReleaseInfo = await _getJSGLatest.GetLatestReleaseInfoAsync("cn.jamsg.SRTools");
            switch (localSettings.Values["Config_UpdateService"])
            {
                case 0:
                    Logging.Write("UService:Github", 0);
                    //latestReleaseInfo = await _getGithubLatest.GetLatestReleaseInfoAsync("JamXi233", "SRToolsHelper");
                    break;
                case 1:
                    Logging.Write("UService:Gitee", 0);
                    latestReleaseInfo = await _getGiteeLatest.GetLatestReleaseInfoAsync("JSG-JamXi", "SRTools");
                    break;
                case 2:
                    Logging.Write("UService:JSG-DS", 0);
                    latestReleaseInfo = await _getJSGLatest.GetLatestReleaseInfoAsync("cn.jamsg.SRTools");
                    break;
                default:
                    Logging.Write($"Invalid update service value: {localSettings.Values["Config_UpdateService"]}", 0);
                    throw new InvalidOperationException($"Invalid update service value: {localSettings.Values["Config_UpdateService"]}");
            }
            Trace.WriteLine(fileUrl);
            string UpdateFileFolder = "\\JSG-LLC\\Updates\\SRTools\\";
            string UpdateFileName = "SRTools_" + latestReleaseInfo.Version + "_x64.zip";
            string UpdateExtractedFolder = "SRTools_" + latestReleaseInfo.Version + "_x64";
            string userDocumentsFolderPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            string localFilePath = Path.Combine(userDocumentsFolderPath + UpdateFileFolder, UpdateFileName);
            ToggleUpdateGridVisibility(false);
            update_Download.IsEnabled = false;
            var progress = new Progress<double>(UpdateReportProgress);
            bool downloadResult = false;
            try
            {
                downloadResult = await _getNetData.DownloadFileWithProgressAsync(fileUrl, localFilePath, progress);
            }
            catch (Exception ex)
            {
                update_Info.Text = ex.Message;
            }

            if (downloadResult)
            {
                Trace.WriteLine(userDocumentsFolderPath);
                string extractionPath = Path.Combine(userDocumentsFolderPath + UpdateFileFolder, UpdateExtractedFolder);
                ZipFile.ExtractToDirectory(localFilePath, extractionPath);
                StorageFile file = await StorageFile.GetFileFromPathAsync(extractionPath + "\\" + UpdateExtractedFolder + ".msix");
                if (file != null)
                {
                    await Launcher.LaunchFileAsync(file);
                    update_Grid.Visibility = Visibility.Visible;
                    update_Progress_Grid.Visibility = Visibility.Collapsed;
                    update_Btn_Text.Text = "请点击安装";
                    update_Btn_Bar.Visibility = Visibility.Collapsed;
                    update_Btn_Icon.Glyph = "&#xea4e";
                }
            }
            else
            {
                // 使用Dispatcher在UI线程上执行ToggleUpdateGridVisibility函数
                ToggleUpdateGridVisibility(true, false);
            }
        }


        private async void DependUpdateDownload_Click(object sender, RoutedEventArgs e)
        {
            _getNetData = new GetNetData();
            ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
            var latestReleaseInfo = await _getJSGLatest.GetLatestReleaseInfoAsync("cn.jamsg.SRTools");
            switch (localSettings.Values["Config_UpdateService"])
            {
                case 0:
                    Logging.Write("UService:Github", 0);
                    //latestReleaseInfo = await _getGithubLatest.GetLatestReleaseInfoAsync("JamXi233", "SRToolsHelper");
                    break;
                case 1:
                    Logging.Write("UService:Gitee", 0);
                    latestReleaseInfo = await _getGiteeLatest.GetLatestReleaseInfoAsync("JSG-JamXi", "SRToolsHelper");
                    break;
                case 2:
                    Logging.Write("UService:JSG-DS", 0);
                    latestReleaseInfo = await _getJSGLatest.GetLatestReleaseInfoAsync("cn.jamsg.SRToolsHelper");
                    break;
                default:
                    Logging.Write($"Invalid update service value: {localSettings.Values["Config_UpdateService"]}", 0);
                    throw new InvalidOperationException($"Invalid update service value: {localSettings.Values["Config_UpdateService"]}");
            }
            Trace.WriteLine(fileUrl);
            string UpdateFileFolder = "\\JSG-LLC\\SRTools\\Depends\\";
            string UpdateFileName = "SRToolsHelper_" + latestReleaseInfo.Version + ".zip";
            string UpdateExtractedFolder = "SRToolsHelper";
            string userDocumentsFolderPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            DeleteFolder(userDocumentsFolderPath + "\\JSG-LLC\\SRTools\\Depends\\", "0");
            string localFilePath = Path.Combine(userDocumentsFolderPath + UpdateFileFolder, UpdateFileName);
            ToggleDependUpdateGridVisibility(false);
            depend_Download.IsEnabled = false;
            var progress = new Progress<double>(UpdateReportProgress);
            bool downloadResult = false;
            try
            {
                downloadResult = await _getNetData.DownloadFileWithProgressAsync(fileUrl, localFilePath, progress);
            }
            catch (Exception ex)
            {
                update_Info.Text = ex.Message;
            }

            if (downloadResult)
            {
                Trace.WriteLine(userDocumentsFolderPath);
                string extractionPath = Path.Combine(userDocumentsFolderPath + UpdateFileFolder, UpdateExtractedFolder);
                ZipFile.ExtractToDirectory(localFilePath, extractionPath);
                MainAPP.Visibility = Visibility.Visible;
                Update.Visibility = Visibility.Collapsed;
            }
            else
            {
                // 使用Dispatcher在UI线程上执行ToggleUpdateGridVisibility函数
                ToggleDependUpdateGridVisibility(true, false);
            }
        }


        private void UpdateDownload_Ignore(object sender, RoutedEventArgs e)
        {
            Update.Visibility = Visibility.Collapsed;
            MainAPP.Visibility = Visibility.Visible;
            checkUpdate.IsEnabled = true;
        }

        private void ToggleUpdateGridVisibility(bool updateGridVisible, bool downloadSuccess = false)
        {
            update_Grid.Visibility = updateGridVisible ? Visibility.Visible : Visibility.Collapsed;
            update_Progress_Grid.Visibility = updateGridVisible ? Visibility.Collapsed : Visibility.Visible;
            update_Btn_Text.Text = downloadSuccess ? "下载完成" : "下载失败";
            update_Btn_Icon.Glyph = downloadSuccess ? "&#xe73a;" : "&#xe8bb;";
        }

        private void ToggleDependUpdateGridVisibility(bool updateGridVisible, bool downloadSuccess = false)
        {
            depend_Grid.Visibility = updateGridVisible ? Visibility.Visible : Visibility.Collapsed;
            depend_Progress_Grid.Visibility = updateGridVisible ? Visibility.Collapsed : Visibility.Visible;
            depend_Btn_Text.Text = downloadSuccess ? "下载完成" : "下载失败";
            depend_Btn_Icon.Glyph = downloadSuccess ? "&#xe73a;" : "&#xe8bb;";
        }

        private void UpdateReportProgress(double progressPercentage)
        {
            update_Btn_Bar.Value = progressPercentage;
            depend_Btn_Bar.Value = progressPercentage;
        }

        private async void Backup_Data(object sender, RoutedEventArgs e)
        {
            DateTime now = DateTime.Now;
            string formattedDate = now.ToString("yyyy_MM_dd_HH_mm_ss");
            Console.WriteLine(formattedDate);
            string userDocumentsFolderPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            var savePicker = new FileSavePicker();
            savePicker.FileTypeChoices.Add("Zip Archive", new List<string>() { ".SRToolsBackup" });
            savePicker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
            savePicker.SuggestedFileName = "SRTools_Backup_" + formattedDate;
            var window = new Window();
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
            WinRT.Interop.InitializeWithWindow.Initialize(savePicker, hwnd);
            StorageFile file = await savePicker.PickSaveFileAsync();
            if (file != null)
            {
                string startPath = userDocumentsFolderPath + @"\JSG-LLC\SRTools";
                string zipPath = file.Path;
                if (File.Exists(zipPath))
                {
                    File.Delete(zipPath);
                }
                ZipFile.CreateFromDirectory(startPath, zipPath);
            }
        }

        private void Restore_Data_Click(object sender, RoutedEventArgs e)
        {
            RestoreTip.IsOpen = true;
        }

        private async void Restore_Data(TeachingTip e, object o)
        {
            var picker = new FileOpenPicker();
            picker.FileTypeFilter.Add(".SRToolsBackup");
            var window = new Window();
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
            WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);
            var file = await picker.PickSingleFileAsync();
            string userDocumentsFolderPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            Task.Run(() => Clear_AllData_NoClose(null, null)).Wait();
            Task.Run(() => ZipFile.ExtractToDirectory(file.Path, userDocumentsFolderPath + "\\JSG-LLC\\SRTools\\")).Wait();
            Application.Current.Exit();
        }

        //Debug_Clicks
        private void Debug_Panic_Click(object sender, RoutedEventArgs e) 
        {
            throw new Exception("全局异常处理测试");
        }
    }
}