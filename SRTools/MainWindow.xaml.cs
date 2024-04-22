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

using SRTools.Views;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using System;
using System.Runtime.InteropServices;
using Vanara.PInvoke;
using Windows.Graphics;
using WinRT.Interop;
using Microsoft.UI.Xaml.Controls;
using Windows.Storage;
using SRTools.Depend;
using System.Threading.Tasks;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using Microsoft.UI.Xaml.Media.Imaging;
using Spectre.Console;
using Windows.System;
using Windows.Storage.AccessCache;
using SRTools.Views.ToolViews;
using SRTools.Views.FirstRunViews;
using System.Data;
using Microsoft.UI.Xaml.Media;
using static SRTools.App;

namespace SRTools
{
    public partial class MainWindow : Window
    {
        private static readonly HttpClient httpClient = new HttpClient();
        private IntPtr hwnd = IntPtr.Zero;
        private OverlappedPresenter presenter;
        private AppWindow appWindow = null;
        private AppWindowTitleBar titleBar;
        string ExpectionFileName;
        string backgroundUrl = "";

        // 导入 AllocConsole 和 FreeConsole 函数
        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool AllocConsole();

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool FreeConsole();
        // 导入 GetAsyncKeyState 函数
        [DllImport("User32.dll")]
        public static extern short GetAsyncKeyState(int vKey);

        public NavigationView NavigationView { get; }

        public MainWindow()
        {
            Windows.ApplicationModel.Core.CoreApplication.UnhandledErrorDetected += OnUnhandledErrorDetected;
            Title = "星轨工具箱";
            InitShiftPress();
            InitializeWindowProperties();
            InitializeComponent();
            NotificationManager.OnNotificationRequested += AddNotification;
            WaitOverlayManager.OnWaitOverlayRequested += ShowWaitOverlay;
            this.Activated += MainWindow_Activated;
            this.Closed += MainWindow_Closed;
        }

        private async void MainWindow_Activated(object sender, WindowActivatedEventArgs args)
        {
            // 确保初始化代码只执行一次
            this.Activated -= MainWindow_Activated;
            await InitializeAppDataAsync();
            await BackgroundImageAsync();
            CleanUpdate();
        }

        private void InitShiftPress()
        {
            bool isShiftPressed = (GetAsyncKeyState(0x10) & 0x8000) != 0;

            if (isShiftPressed)
            {
                Logging.Write("已通过快捷键进入控制台模式", 1);
                Console.Title = "🆂𝐃𝐞𝐛𝐮𝐠𝐌𝐨𝐝𝐞:SRTools";
                TerminalMode.ShowConsole();
                SDebugMode = true;
            }
            else
            {
                Logging.Write("NoPressed", 1);
            }
        }

        private async Task InitializeAppDataAsync()
        {
            AppDataController appDataController = new AppDataController();

            Logo_Progress.Visibility = Visibility.Visible;
            Logo.Visibility = Visibility.Collapsed;
            MainNavigationView.Visibility = Visibility.Visible;

            if (AppDataController.GetFirstRun() == 1)
            {
                FirstRun_Frame.Navigate(typeof(FirstRunAnimation));
                await Task.Delay(1000);

                if (appDataController.CheckOldData() == 1)
                {
                    FirstRunAnimation.isOldDataExist = true;
                    StartCheckingFirstRun();
                }
                else
                {
                    InitFirstRun();
                }

                MainAPP.Visibility = Visibility.Collapsed;
            }
            else
            {
                KillFirstUI();
            }
        }

        private void InitFirstRun()
        {
            StartCheckingFirstRun();
            Logo_Progress.Visibility = Visibility.Collapsed;
            Logo.Visibility = Visibility.Visible;
            MainNavigationView.Visibility = Visibility.Visible;

            int firstRunStatus = AppDataController.GetFirstRunStatus();

            switch (firstRunStatus)
            {
                case 1:
                case 0:
                case -1:
                    FirstRun_Frame.Navigate(typeof(FirstRunInit));
                    break;
                case 2:
                    FirstRun_Frame.Navigate(typeof(FirstRunTheme));
                    break;
                case 3:
                    FirstRun_Frame.Navigate(typeof(FirstRunSourceSelect));
                    break;
                case 4:
                    FirstRun_Frame.Navigate(typeof(FirstRunGetDepend));
                    break;
                case 5:
                    FirstRun_Frame.Navigate(typeof(FirstRunExtra));
                    break;
                default:
                    Logging.Write($"Unknown FirstRunStatus: {firstRunStatus}", 2);
                    FirstRun_Frame.Navigate(typeof(FirstRunInit));
                    break;
            }
        }


        public void StartCheckingFirstRun()
        {
            DispatcherTimer timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(0.1);
            timer.Tick += Timer_Tick;
            timer.Start();
        }

        private void Timer_Tick(object sender, object e)
        {
            if (AppDataController.GetFirstRun() == 0)
            {
                KillFirstUI();
                // 停止计时器，因为不再需要检查
                (sender as DispatcherTimer)?.Stop();
            }
        }

        public void KillFirstUI()
        {
            MainNavigationView.Visibility = Visibility.Collapsed;
            MainAPP.Visibility = Visibility.Visible;
        }

        private void InitializeWindowProperties()
        {
            //设置窗口大小等开始
            hwnd = WindowNative.GetWindowHandle(this);
            WindowId id = Win32Interop.GetWindowIdFromWindow(hwnd);
            appWindow = AppWindow.GetFromWindowId(id);
            DisableWindowResize();
            // 设置窗口为不可调整大小
            presenter = appWindow.Presenter as OverlappedPresenter;
            if (presenter != null)
            {
                // 禁用最大化和大小调整
                presenter.IsResizable = false;
                presenter.IsMaximizable = false;
            }

            float scale = (float)User32.GetDpiForWindow(hwnd) / 96;

            int windowX = (int)(560 * scale);
            int windowY = (int)(280 * scale);
            int windowWidth = (int)(1024 * scale);
            int windowHeight = (int)(584 * scale);

            Logging.Write("MoveAndResize to " + windowWidth + "*" + windowHeight, 0);
            appWindow.MoveAndResize(new RectInt32(windowX, windowY, windowWidth, windowHeight));

            if (AppWindowTitleBar.IsCustomizationSupported())
            {
                titleBar = appWindow.TitleBar;
                titleBar.ExtendsContentIntoTitleBar = true;
                titleBar.ButtonBackgroundColor = Colors.Transparent;
                titleBar.ButtonInactiveBackgroundColor = Colors.Transparent;
                if (App.CurrentTheme == ApplicationTheme.Light) titleBar.ButtonForegroundColor = Colors.Black;
                else titleBar.ButtonForegroundColor = Colors.White;

                titleBar.SetDragRectangles(new RectInt32[] { new RectInt32((int)(48 * scale), 0, 10000, (int)(48 * scale)) });
                Logging.Write("SetDragRectangles to " + 48 * scale + "*" + 48 * scale, 0);
            }
            else
            {
                ExtendsContentIntoTitleBar = true;
                SetTitleBar(AppTitleBar);
            }

            if (AppDataController.GetDayNight() == 0)
            {
                RegisterSystemThemeChangeEvents(id);
            }

        }


        private void RegisterSystemThemeChangeEvents(WindowId id)
        {
            var uiSettings = new Windows.UI.ViewManagement.UISettings();
            uiSettings.ColorValuesChanged += (sender, args) =>
            {
                if (appWindow == null) return;
                UpdateTitleBarColor(appWindow.TitleBar);
            };

            // 初始化时也设置一次标题栏颜色
            UpdateTitleBarColor(appWindow.TitleBar);
        }

        private void UpdateTitleBarColor(AppWindowTitleBar titleBar)
        {
            if (titleBar == null) return;

            // 如果 AppDataController.GetDayNight() 返回 0, 使用系统主题颜色
            if (AppDataController.GetDayNight() == 0)
            {
                var uiSettings = new Windows.UI.ViewManagement.UISettings();
                var foregroundColor = uiSettings.GetColorValue(Windows.UI.ViewManagement.UIColorType.Foreground);
                titleBar.ButtonForegroundColor = foregroundColor;
            }
            else
            {
                // 否则，根据 App.CurrentTheme 设置颜色
                titleBar.ButtonForegroundColor = App.CurrentTheme == ApplicationTheme.Light ? Colors.Black : Colors.White;
            }
        }


        private void DisableWindowResize()
        {
            int style = NativeMethods.GetWindowLong(hwnd, NativeMethods.GWL_STYLE);

            // Remove the WS_SIZEBOX style to disable resizing
            style &= ~NativeMethods.WS_SIZEBOX;
            NativeMethods.SetWindowLong(hwnd, NativeMethods.GWL_STYLE, style);
        }

        private async Task BackgroundImageAsync()
        {
            string apiUrl = "https://api-launcher-static.mihoyo.com/hkrpg_cn/mdk/launcher/api/content?filter_adv=false&key=6KcVuOkbcqjJomjZ&language=zh-cn&launcher_id=33";
            ApiResponse response = await FetchData(apiUrl);
            backgroundUrl = response.data.adv.background;

            BitmapImage backgroundImage = new BitmapImage();
            backgroundImage.UriSource = new Uri(backgroundUrl);
            Background.ImageSource = backgroundImage;
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
                    case "flarum":
                        MainFrame.Navigate(typeof(FlarumView));
                        break;
                    case "donation":
                        MainFrame.Navigate(typeof(DonationView));
                        break;
                    case "account_status":
                        MainFrame.Navigate(typeof(AccountStatusView));
                        break;
                    case "settings":
                        MainFrame.Navigate(typeof(AboutView));
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

                // 显示InfoBar通知
                var errorMessage = ex.Message.Trim() + "\n\n已生成错误报告\n如再次尝试仍会重现错误\n您可以到Github提交Issue";
                // 调用 AddNotification 方法来显示错误信息和操作按钮
                AddNotification("严重错误", errorMessage, InfoBarSeverity.Error, () =>
                {
                    ExpectionFolderOpen_Click();
                }, "打开文件夹");

                AnsiConsole.WriteException(ex, ExceptionFormats.ShortenPaths | ExceptionFormats.ShortenTypes | ExceptionFormats.ShortenMethods | ExceptionFormats.ShowLinks | ExceptionFormats.Default);
                await ExceptionSave.Write("源:" + ex.Source + "\n错误标题:" + ex.Message + "\n堆栈跟踪:\n" + ex.StackTrace + "\n内部异常:\n" + ex.InnerException + "\n结束代码:" + ex.HResult, 1, ExpectionFileName);
            }
        }

        private async void ExpectionFolderOpen_Click() 
        {
            StorageFolder folder = await KnownFolders.DocumentsLibrary.CreateFolderAsync("JSG-LLC\\Panic", CreationCollisionOption.OpenIfExists);
            // 获取指定文件
            StorageFile file = await folder.GetFileAsync(ExpectionFileName);
            // 将文件添加到最近使用列表中
            StorageApplicationPermissions.MostRecentlyUsedList.Add(file);
            await Launcher.LaunchFolderAsync(folder, new FolderLauncherOptions { ItemsToSelect = { file } });
        }


        public void AddNotification(string title, string message, InfoBarSeverity severity, Action actionButtonAction = null, string actionButtonText = null)
        {
            var infoBar = new InfoBar
            {
                Title = title,
                Message = message,
                Severity = severity,
                IsOpen = true,
                VerticalAlignment = Microsoft.UI.Xaml.VerticalAlignment.Top,
                Margin = new Thickness(0, 0, 0, 5),
            };
            if (actionButtonText != null)
            {
                var actionButton = new Button { Content = actionButtonText };
                if (actionButtonAction != null)
                {
                    actionButton.Click += (sender, args) => actionButtonAction.Invoke();
                }
                infoBar.ActionButton = actionButton;
            }
            infoBar.CloseButtonClick += (sender, args) =>
            {
                InfoBarPanel.Children.Remove(sender as InfoBar);
            };
            InfoBarPanel.Children.Add(infoBar);
            Logging.WriteNotification(title, message);
        }

        public void ShowWaitOverlay(bool status, bool isProgress = false, string title = null, string subtitle = null)
        {
            if (status)
            {
                WaitOverlay.Visibility = Visibility.Visible;
                if (isProgress) WaitOverlay_Progress.Visibility = Visibility.Visible;
                else WaitOverlay_Success.Visibility = Visibility.Visible;
                WaitOverlay_Title.Text = title;
                WaitOverlay_SubTitle.Text = subtitle;
            }
            else WaitOverlay.Visibility = Visibility.Collapsed;
        }

        private void MainWindow_Closed(object sender, WindowEventArgs e)
        {
            NotificationManager.OnNotificationRequested -= AddNotification;
            WaitOverlayManager.OnWaitOverlayRequested -= ShowWaitOverlay;
        }

    }
}