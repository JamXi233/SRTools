using SRTools.Views;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Scighost.WinUILib.Helpers;
using System;
using System.Runtime.InteropServices;
using Vanara.PInvoke;
using Windows.Graphics;
using WinRT.Interop;
using Microsoft.UI.Xaml.Controls;
using Windows.Storage;
using SRTools.Depend;
using System.IO.Compression;
using System.Threading.Tasks;
using System.IO;
using System.Diagnostics;
using System.Net.Http;
using System.Text.Json;
using Microsoft.UI.Xaml.Media.Imaging;
using System.Threading;
using System.Security.AccessControl;
using Spectre.Console;
using Windows.System;
using Windows.UI.Core;
using Org.BouncyCastle.Asn1.X509;
using Windows.Storage.AccessCache;

namespace SRTools
{
    public partial class MainWindow : Window
    {

        private GetNetData _getNetData;
        private readonly GetGiteeLatest _getGiteeLatest = new GetGiteeLatest();
        private readonly GetJSGLatest _getJSGLatest = new GetJSGLatest();
        private static readonly HttpClient httpClient = new HttpClient();
        private IntPtr hwnd;
        string fileUrl;
        private AppWindow appWindow;
        private AppWindowTitleBar titleBar;
        private SystemBackdrop backdrop;
        string ExpectionFileName;
        string backgroundUrl = "";

        private const string KeyPath = "SRTools";
        private const string ValueFirstRun = "Config_FirstRun";
        private const string ValueGamePath = "Config_GamePath";
        private const string ValueUnlockFPS = "Config_UnlockFPS";
        private const string ValueUpdateService = "Config_UpdateService";
        // 导入 AllocConsole 和 FreeConsole 函数
        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool AllocConsole();

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool FreeConsole();

        private record LocalSettingsData(string FirstRun, string GamePath, string UnlockFPSValue, string UpdateService);

        public NavigationView NavigationView { get; }

        public MainWindow()
        {
            Windows.ApplicationModel.Core.CoreApplication.UnhandledErrorDetected += OnUnhandledErrorDetected;
            Title = "星轨工具箱";
            InitializeComponent();
            InitializeAppData();
            InitializeMicaBackground();
            InitializeWindowProperties();
            BackgroundImage();
            CleanUpdate();

            _getNetData = new GetNetData();
        }

        private void InitializeAppData()
        {
            //应用数据检查开始
            ApplicationDataContainer keyContainer = GetOrCreateContainer(KeyPath);
            LocalSettingsData defaultValues = new LocalSettingsData("1", "Null", "Null", "Null");
            LocalSettingsData currentValues = GetCurrentValues(keyContainer, defaultValues);
            if (currentValues.FirstRun == "1")
            {
                FirstRun.Visibility = Visibility.Visible;
                MainAPP.Visibility = Visibility.Collapsed;
            }
            else if (currentValues.FirstRun == "0")
            {
                FirstRun.Visibility = Visibility.Collapsed;
                MainAPP.Visibility = Visibility.Visible;
            }
        }

        private void InitializeMicaBackground()
        {
            //设置背景Mica开始
            backdrop = new SystemBackdrop(this);
            backdrop.TrySetMica(fallbackToAcrylic: true);
        }

        private void InitializeWindowProperties()
        {
            //设置窗口大小等开始
            hwnd = WindowNative.GetWindowHandle(this);
            WindowId id = Win32Interop.GetWindowIdFromWindow(hwnd);
            appWindow = AppWindow.GetFromWindowId(id);
            DisableWindowResize();

            float scale = (float)User32.GetDpiForWindow(hwnd) / 96;

            int windowX = (int)(560 * scale);
            int windowY = (int)(280 * scale);
            int windowWidth = (int)(1024 * scale);
            int windowHeight = (int)(584 * scale);

            Logging.Write("MoveAndResize to "+windowWidth+"*"+windowHeight,0);
            appWindow.MoveAndResize(new RectInt32(windowX, windowY, windowWidth, windowHeight));

            if (AppWindowTitleBar.IsCustomizationSupported())
            {
                titleBar = appWindow.TitleBar;
                titleBar.ExtendsContentIntoTitleBar = true;
                titleBar.ButtonBackgroundColor = Colors.Transparent;
                titleBar.ButtonInactiveBackgroundColor = Colors.Transparent;
                titleBar.ButtonForegroundColor = Colors.White;

                titleBar.SetDragRectangles(new RectInt32[] { new RectInt32((int)(48 * scale), 0, 10000, (int)(48 * scale)) });
                Logging.Write("SetDragRectangles to "+48 * scale+"*"+ 48 * scale, 0);
            }
            else
            {
                ExtendsContentIntoTitleBar = true;
                SetTitleBar(AppTitleBar);
            }
        }

        private void DisableWindowResize()
        {
            int style = NativeMethods.GetWindowLong(hwnd, NativeMethods.GWL_STYLE);

            // Remove the WS_SIZEBOX style to disable resizing
            style &= ~NativeMethods.WS_SIZEBOX;
            NativeMethods.SetWindowLong(hwnd, NativeMethods.GWL_STYLE, style);
        }
        
        private async void BackgroundImage()
        {
            string apiUrl = "https://api-launcher-static.mihoyo.com/hkrpg_cn/mdk/launcher/api/content?filter_adv=false&key=6KcVuOkbcqjJomjZ&language=zh-cn&launcher_id=33";
            ApiResponse response = await FetchData(apiUrl);
            backgroundUrl = response.data.adv.background;
            // 设置背景图片
            Logging.Write("Getting Background: " + backgroundUrl, 0);
            BitmapImage backgroundImage = new BitmapImage(new Uri(backgroundUrl));
            Background.ImageSource = backgroundImage;
        }

        //应用数据检查开始
        private ApplicationDataContainer GetOrCreateContainer(string keyPath)
        {
            ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
            switch (localSettings.Values["Config_TerminalMode"])
            {
                case 0:
                    TerminalMode.HideConsole();
                    break;
                case 1:
                    TerminalMode.ShowConsole();
                    break;
                default:
                    TerminalMode.HideConsole();
                    break;
            }
            if (localSettings.Containers.ContainsKey(keyPath))
            {
                return localSettings.Containers[keyPath];
            }
            else
            {
                return localSettings.CreateContainer(keyPath, ApplicationDataCreateDisposition.Always);
            }
        }

        // 获取当前设置值或默认值
        private LocalSettingsData GetCurrentValues(ApplicationDataContainer keyContainer, LocalSettingsData defaultValues)
        {
            string firstRunValue = GetValueOrDefault(keyContainer, ValueFirstRun, defaultValues.FirstRun);
            string gamePathValue = GetValueOrDefault(keyContainer, ValueGamePath, defaultValues.GamePath);
            string unlockFPSValue = GetValueOrDefault(keyContainer, ValueUnlockFPS, defaultValues.UnlockFPSValue);
            string updateService = GetValueOrDefault(keyContainer, ValueUpdateService, defaultValues.UpdateService);

            return new LocalSettingsData(firstRunValue, gamePathValue, unlockFPSValue, updateService);
        }

        // 获取键值对应的值，如果不存在则使用默认值
        private string GetValueOrDefault(ApplicationDataContainer keyContainer, string key, string defaultValue)
        {
            if (keyContainer.Values.ContainsKey(key))
            {
                return (string)keyContainer.Values[key];
            }
            else
            {
                return defaultValue;
            }
        }

        //选择下载渠道开始
        private void DSerivceChoose_Click(object sender, RoutedEventArgs e)
        {
            dservice_panel_left.Visibility = Visibility.Collapsed;
            dservice_panel_right.Visibility = Visibility.Collapsed;
            depend_panel_left.Visibility = Visibility.Visible;
            depend_panel_right.Visibility = Visibility.Visible;
            OnGetDependLatestReleaseInfo();
        }

        private void DService_Github_Choose(object sender, RoutedEventArgs e)
        {
            ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
            localSettings.Values["Config_UpdateService"] = 0;
            dserivce_finish.IsEnabled = true;
        }

        private void DService_Gitee_Choose(object sender, RoutedEventArgs e)
        {
            ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
            localSettings.Values["Config_UpdateService"] = 1;
            dserivce_finish.IsEnabled = true;
        }

        private void DService_JSG_Choose(object sender, RoutedEventArgs e)
        {
            ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
            localSettings.Values["Config_UpdateService"] = 2;
            dserivce_finish.IsEnabled = true;
        }

        //依赖下载开始

        private const string FileFolder = "\\JSG-LLC\\SRTools\\Depends";
        private const string ZipFileName = "SRToolsHelper.zip";
        private const string ExtractedFolder = "SRToolsHelper";

        private async void DependDownload_Click(object sender, RoutedEventArgs e)
        {
            string userDocumentsFolderPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            string localFilePath = Path.Combine(userDocumentsFolderPath + FileFolder, ZipFileName);
            Trace.WriteLine(fileUrl);
            ToggleDependGridVisibility(false);
            depend_Download.IsEnabled = false;
            var progress = new Progress<double>(DependReportProgress);
            bool downloadResult = false;
            try
            {
                downloadResult = await _getNetData.DownloadFileWithProgressAsync(fileUrl, localFilePath, progress);
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }

            if (downloadResult)
            {
                string keyPath = "SRTools";
                ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
                ApplicationDataContainer keyContainer = localSettings.Containers[keyPath];
                string extractionPath = Path.Combine(userDocumentsFolderPath + FileFolder, ExtractedFolder);
                if (File.Exists(extractionPath+ "\\SRToolsHelper.exe"))
                {
                    string valueFirstRun = "Config_FirstRun";
                    FirstRun.Visibility = Visibility.Collapsed;
                    MainAPP.Visibility = Visibility.Visible;
                    keyContainer.Values[valueFirstRun] = "0";
                }
                else 
                {
                    string valueFirstRun = "Config_FirstRun";
                    Trace.WriteLine(userDocumentsFolderPath);
                    Trace.WriteLine(extractionPath);
                    ZipFile.ExtractToDirectory(localFilePath, extractionPath);
                    FirstRun.Visibility = Visibility.Collapsed;
                    MainAPP.Visibility = Visibility.Visible;
                    keyContainer.Values[valueFirstRun] = "0";
                }
            }
            else
            {
                // 使用Dispatcher在UI线程上执行ToggleUpdateGridVisibility函数
                ToggleDependGridVisibility(true, false);
            }
        }

        private void ToggleDependGridVisibility(bool updateGridVisible, bool downloadSuccess = false)
        {
            depend_Grid.Visibility = updateGridVisible ? Visibility.Visible : Visibility.Collapsed;
            depend_Progress_Grid.Visibility = updateGridVisible ? Visibility.Collapsed : Visibility.Visible;
            depend_Btn_Text.Text = downloadSuccess ? "下载完成" : "下载失败";
            depend_Btn_Icon.Glyph = downloadSuccess ? "&#xe73a;" : "&#xe8bb;";
        }

        private async void OnGetDependLatestReleaseInfo()
        {
            depend_Grid.Visibility = Visibility.Collapsed;
            depend_Progress_Grid.Visibility = Visibility.Visible;
            var dispatcher = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();
            try
            {
                ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
                var latestReleaseInfo = await _getJSGLatest.GetLatestReleaseInfoAsync("cn.jamsg.srtoolshelper");
                switch (localSettings.Values["Config_UpdateService"])
                {
                    case 0:
                        //latestReleaseInfo = await _getGithubLatest.GetLatestReleaseInfoAsync("JamXi233", "SRToolsHelper");
                        break;
                    case 1:
                        latestReleaseInfo = await _getGiteeLatest.GetLatestReleaseInfoAsync("JSG-JamXi", "SRToolsHelper");
                        break;
                    case 2:
                        latestReleaseInfo = await _getJSGLatest.GetLatestReleaseInfoAsync("cn.jamsg.srtoolshelper");
                        break;
                    default:
                        throw new InvalidOperationException($"Invalid update service value: {localSettings.Values["Config_UpdateService"]}");
                }

                fileUrl = latestReleaseInfo.DownloadUrl;
                dispatcher.TryEnqueue(() =>
                {
                    depend_Latest_Name.Text = $"依赖项: {latestReleaseInfo.Name}";
                    depend_Latest_Version.Text = $"版本号: {latestReleaseInfo.Version}";
                    depend_Download.IsEnabled = true;
                    depend_Grid.Visibility = Visibility.Visible;
                    depend_Progress_Grid.Visibility = Visibility.Collapsed;
                });
            }
            catch (Exception ex)
            {
                dispatcher.TryEnqueue(() =>
                {
                    depend_Grid.Visibility = Visibility.Visible;
                    depend_Progress_Grid.Visibility = Visibility.Collapsed;
                    depend_Btn_Text.Text = "获取失败";
                    depend_Latest_Version.Text = ex.Message;
                    depend_Btn_Bar.Visibility = Visibility.Collapsed;
                    depend_Btn_Icon.Glyph = "&#xe8bb" ;
                });
            }
        }

        private void DependReportProgress(double progressPercentage)
        {
            depend_Btn_Bar.Value = progressPercentage;
        }

        private void NavView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
            NavigationViewItem item = args.SelectedItem as NavigationViewItem;
            string tag = item.Tag.ToString();
            if (args.IsSettingsSelected)
            {
                MainFrame.Navigate(typeof(AboutView));
            }
            else if (args.SelectedItemContainer != null)
            {
                switch (args.SelectedItemContainer.Tag.ToString())
                {
                    case "home":
                        MainFrame.Navigate(typeof(MainView));
                        break;
                    case "startgame":
                        MainFrame.Navigate(typeof(StartGameView));
                        break;
                    case "gacha":
                        MainFrame.Navigate(typeof(GachaView));
                        break;
                    case "question":
                        MainFrame.Navigate(typeof(QuestionView));
                        break;
                    case "donation":
                        MainFrame.Navigate(typeof(DonationView));
                        break;
                }
            }
        }
        public static async Task<ApiResponse> FetchData(string url)
        {
            HttpResponseMessage httpResponse = await httpClient.GetAsync(url);
            httpResponse.EnsureSuccessStatusCode();
            string responseBody = await httpResponse.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<ApiResponse>(responseBody);
        }
        public class ApiResponse
        {
            public int retcode { get; set; }
            public string message { get; set; }
            public Data data { get; set; }
        }

        private void CleanUpdate() 
        {
            string folderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "JSG-LLC", "Updates");
            if (Directory.Exists(folderPath)) Directory.Delete(folderPath, true);
        }

        private async void OnUnhandledErrorDetected(object sender, Windows.ApplicationModel.Core.UnhandledErrorDetectedEventArgs e)
        {
            try
            {
                e.UnhandledError.Propagate();
            }
            catch (Exception ex)
            {
                ExpectionFileName = string.Format("SRTools_Panic_{0:yyyyMMdd_HHmmss}.SRToolsPanic", DateTime.Now);
                infoBar.IsOpen = true;
                infoBar.Title = "严重错误";
                infoBar.Message = ex.Message.Trim()+"\n\n已生成错误报告\n如再次尝试仍会重现错误\n您可以到Github提交Issue";
                AnsiConsole.WriteException(ex,ExceptionFormats.ShortenPaths | ExceptionFormats.ShortenTypes | ExceptionFormats.ShortenMethods | ExceptionFormats.ShowLinks);
                await ExceptionSave.Write("应用程序:" + ex.Source+"\n错误标题:"+ex.Message + "\n堆栈跟踪:\n" + ex.StackTrace + "\n结束代码:" + ex.HResult, 1,ExpectionFileName);
            }
        }

        private async void ExpectionFolderOpen_Click(object sender, RoutedEventArgs e) 
        {
            StorageFolder folder = await KnownFolders.DocumentsLibrary.CreateFolderAsync("JSG-LLC\\Panic", CreationCollisionOption.OpenIfExists);
            // 获取指定文件
            StorageFile file = await folder.GetFileAsync(ExpectionFileName);
            // 将文件添加到最近使用列表中
            StorageApplicationPermissions.MostRecentlyUsedList.Add(file);
            await Launcher.LaunchFolderAsync(folder, new FolderLauncherOptions { ItemsToSelect = { file } });
        }
    }
}