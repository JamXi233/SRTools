// 版权信息
// 使用 MIT 许可证。
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
using Microsoft.Win32;
using Windows.Storage;
using SRTools.Depend;
using System.IO.Compression;
using Windows.Devices.Geolocation;
using System.Threading.Tasks;
using System.IO;
using Windows.UI.Core;
using System.Diagnostics;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.
namespace SRTools
{
    public partial class MainWindow : Window
    {
        private GetNetData _getNetData;
        private readonly GetGiteeLatest _getGithubLatest = new GetGiteeLatest();
        string fileUrl;
        private IntPtr hwnd;
        private AppWindow appWindow;
        private AppWindowTitleBar titleBar;
        private SystemBackdrop backdrop;

        private const string KeyPath = "SRTools";
        private const string ValueFirstRun = "Config_FirstRun";
        private const string ValueGamePath = "Config_Folder";
        private const string ValueUnlockFPS = "Config_UnlockFPS";

        private record LocalSettingsData(string FirstRun, string UnlockFPSValue);

        public MainWindow()
        {
            this.Title = "星轨工具箱";
            this.InitializeComponent();

            _getNetData = new GetNetData();
            ApplicationDataContainer keyContainer = GetOrCreateContainer(KeyPath);

            LocalSettingsData defaultValues = new LocalSettingsData("1", "Null");
            LocalSettingsData currentValues = GetCurrentValues(keyContainer, defaultValues);

            if (currentValues.FirstRun == "1")
            {
                Update.Visibility = Visibility.Collapsed;
                FirstRun.Visibility = Visibility.Visible;
                MainAPP.Visibility = Visibility.Collapsed;
                OnGetLatestReleaseInfo();
            }
            else if (currentValues.FirstRun == "0")
            {
                Update.Visibility = Visibility.Collapsed;
                FirstRun.Visibility = Visibility.Collapsed;
                MainAPP.Visibility = Visibility.Visible;
            }

            backdrop = new SystemBackdrop(this);
            backdrop.TrySetMica(fallbackToAcrylic: true);

            hwnd = WindowNative.GetWindowHandle(this);
            WindowId id = Win32Interop.GetWindowIdFromWindow(hwnd);
            appWindow = AppWindow.GetFromWindowId(id);

            float scale = (float)User32.GetDpiForWindow(hwnd) / 96;

            int windowX = (int)(560 * scale);
            int windowY = (int)(280 * scale);
            int windowWidth = (int)(1024 * scale);
            int windowHeight = (int)(584 * scale);

            appWindow.MoveAndResize(new RectInt32(windowX, windowY, windowWidth, windowHeight));

            if (AppWindowTitleBar.IsCustomizationSupported())
            {
                titleBar = appWindow.TitleBar;
                titleBar.ExtendsContentIntoTitleBar = true;
                titleBar.ButtonBackgroundColor = Colors.Transparent;
                titleBar.ButtonInactiveBackgroundColor = Colors.Transparent;
                titleBar.ButtonForegroundColor = Colors.White;

                titleBar.SetDragRectangles(new RectInt32[] { new RectInt32((int)(48 * scale), 0, 10000, (int)(48 * scale)) });
            }
            else
            {
                ExtendsContentIntoTitleBar = true;
                SetTitleBar(AppTitleBar);
            }
        }

        private ApplicationDataContainer GetOrCreateContainer(string keyPath)
        {
            ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
            return localSettings.Containers.ContainsKey(keyPath)
                ? localSettings.Containers[keyPath]
                : localSettings.CreateContainer(keyPath, ApplicationDataCreateDisposition.Always);
        }

        private LocalSettingsData GetCurrentValues(ApplicationDataContainer keyContainer, LocalSettingsData defaultValues)
        {
            return new LocalSettingsData(
                GetValueOrDefault(keyContainer, ValueFirstRun, defaultValues.FirstRun),
                GetValueOrDefault(keyContainer, ValueUnlockFPS, defaultValues.UnlockFPSValue)
            );
        }

        private string GetValueOrDefault(ApplicationDataContainer keyContainer, string key, string defaultValue)
        {
            return keyContainer.Values.ContainsKey(key) ? (string)keyContainer.Values[key] : defaultValue;
        }

        private const string FileFolder = "\\JSG-LLC\\SRTools\\Depends";
        private const string ZipFileName = "SRToolsHelper.zip";
        private const string ExtractedFolder = "SRToolsHelper";

        private async void DependDownload_Click(object sender, RoutedEventArgs e)
        {
            string userDocumentsFolderPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            string localFilePath = Path.Combine(userDocumentsFolderPath + FileFolder, ZipFileName);
            Trace.WriteLine(fileUrl);

            // 获取UI线程的DispatcherQueue
            var dispatcher = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();

            // 使用Dispatcher在UI线程上执行ToggleUpdateGridVisibility函数
            dispatcher.TryEnqueue(() => ToggleUpdateGridVisibility(false));

            var progress = new Progress<double>(ReportProgress);
            try
            {
                bool downloadResult = await _getNetData.DownloadFileWithProgressAsync(fileUrl, localFilePath, progress);
                if (downloadResult)
                {
                    string keyPath = "SRTools";
                    ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
                    ApplicationDataContainer keyContainer = localSettings.Containers[keyPath];
                    string valueFirstRun = "Config_FirstRun";
                    Trace.WriteLine(userDocumentsFolderPath);
                    string extractionPath = Path.Combine(userDocumentsFolderPath + FileFolder, ExtractedFolder);
                    Trace.WriteLine(extractionPath);
                    ZipFile.ExtractToDirectory(localFilePath, extractionPath);
                    Update.Visibility = Visibility.Collapsed;
                    FirstRun.Visibility = Visibility.Collapsed;
                    MainAPP.Visibility = Visibility.Visible;
                    keyContainer.Values[valueFirstRun] = "0";
                    // 使用Dispatcher在UI线程上执行ToggleUpdateGridVisibility函数
                    dispatcher.TryEnqueue(() => ToggleUpdateGridVisibility(true, true));
                }
                else
                {
                    // 使用Dispatcher在UI线程上执行ToggleUpdateGridVisibility函数
                    dispatcher.TryEnqueue(() => ToggleUpdateGridVisibility(true, false));
                }
            }
            catch (Exception ex)
            {
                update_Info.Text = ex.Message;
            }
        }

        private void ToggleUpdateGridVisibility(bool updateGridVisible, bool downloadSuccess = false)
        {
            update_Grid.Visibility = updateGridVisible ? Visibility.Visible : Visibility.Collapsed;
            update_Progress_Grid.Visibility = updateGridVisible ? Visibility.Collapsed : Visibility.Visible;
            update_Btn_Text.Text = downloadSuccess ? "下载完成" : "下载失败";
            update_Btn_Bar.Visibility = Visibility.Collapsed;
            update_Btn_Icon.Symbol = downloadSuccess ? Symbol.Accept : Symbol.Stop;
        }

        private async void OnGetLatestReleaseInfo()
        {
            var dispatcher = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();
            try
            {
                var latestReleaseInfo = await _getGithubLatest.GetLatestReleaseInfoAsync("JSG-JamXi", "SRToolsHelper");
                fileUrl = latestReleaseInfo.DownloadUrl;
                dispatcher.TryEnqueue(() =>
                {
                    depend_Latest_Version.Text = $"版本号: {latestReleaseInfo.Version}";
                    depend_Download.IsEnabled = true;
                    update_Grid.Visibility = Visibility.Visible;
                    update_Progress_Grid.Visibility = Visibility.Collapsed;
                });
            }
            catch (Exception ex)
            {
                dispatcher.TryEnqueue(() =>
                {
                    update_Grid.Visibility = Visibility.Visible;
                    update_Progress_Grid.Visibility = Visibility.Collapsed;
                    update_Btn_Text.Text = "获取失败";
                    depend_Latest_Version.Text = ex.Message;
                    update_Btn_Bar.Visibility = Visibility.Collapsed;
                    update_Btn_Icon.Symbol = Symbol.Stop;
                });
            }
        }

        private void ReportProgress(double progressPercentage)
        {
            // 更新进度条
            update_Btn_Bar.Value = progressPercentage;
        }

        private void NavView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
            if (args.IsSettingsSelected)
            {
                // 处理设置菜单项单击事件
            }
            else if (args.SelectedItemContainer != null)
            {
                switch (args.SelectedItemContainer.Tag.ToString())
                {
                    case "home":
                        // 导航到主页
                        MainFrame.Navigate(typeof(MainView));
                        break;
                    case "startgame":
                        // 导航到启动游戏页
                        MainFrame.Navigate(typeof(StartGameView));
                        break;
                    case "gacha":
                        // 导航到启动游戏页
                        MainFrame.Navigate(typeof(GachaView));
                        break;
                }
            }
            if (args.IsSettingsSelected)
            {
                // 导航到默认设置页面
                MainFrame.Navigate(typeof(AboutView));
            }
        }

    }
}