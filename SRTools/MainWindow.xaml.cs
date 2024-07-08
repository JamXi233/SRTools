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
using SRTools.Depend;
using System.Threading.Tasks;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.System;
using SRTools.Views.FirstRunViews;
using static SRTools.App;
using System.Diagnostics;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Media;

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

        private Action buttonAction;
        private static bool isDialogOpen = false;

        private MainFrameController mainFrameController;

        public MainWindow()
        {
            Windows.ApplicationModel.Core.CoreApplication.UnhandledErrorDetected += OnUnhandledErrorDetected;
            Title = "星轨工具箱";
            InitShiftPress();
            InitializeWindowProperties();
            InitializeComponent();

            NotificationManager.OnNotificationRequested += AddNotification;
            WaitOverlayManager.OnWaitOverlayRequested += ShowWaitOverlay;
            DialogManager.OnDialogRequested += ShowDialog;
            mainFrameController = new MainFrameController(MainFrame);

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
            if (AppDataController.GetAutoCheckUpdate() == 1)
            {
                await AutoGetUpdate();
            }
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

        private async Task AutoGetUpdate()
        {
            var result = await GetUpdate.GetDependUpdate();
            var status = result.Status;
            if (status == 1)
            {
                NotificationManager.RaiseNotification("更新提示", "依赖包需要更新\n请尽快到[设置-检查依赖更新]进行更新", InfoBarSeverity.Warning, false, 5);
            }
            result = await GetUpdate.GetSRToolsUpdate();
            status = result.Status;
            if (status == 1)
            {
                NotificationManager.RaiseNotification("更新提示", "SRTools有更新\n可到[设置-检查更新]进行更新", InfoBarSeverity.Warning, false, 5);
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

            int windowWidth = (int)(1024 * scale);
            int windowHeight = (int)(584 * scale);

            Logging.Write("Resize to " + windowWidth + "*" + windowHeight, 0);
            appWindow.Resize(new SizeInt32(windowWidth, windowHeight));

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
            string apiUrl = "https://hyp-api.mihoyo.com/hyp/hyp-connect/api/getGames?launcher_id=jGHBHlcOq1&language=zh-cn";
            string responseBody = await FetchData(apiUrl);
            using (JsonDocument doc = JsonDocument.Parse(responseBody))
            {
                JsonElement root = doc.RootElement;
                JsonElement games = root.GetProperty("data").GetProperty("games");

                foreach (JsonElement game in games.EnumerateArray())
                {
                    if (game.GetProperty("biz").GetString() == "hkrpg_cn")
                    {
                        backgroundUrl = game.GetProperty("display").GetProperty("background").GetProperty("url").GetString();
                        break;
                    }
                }
            }

            if (!string.IsNullOrEmpty(backgroundUrl))
            {
                BitmapImage backgroundImage = new BitmapImage();
                backgroundImage.UriSource = new Uri(backgroundUrl);
                Background.ImageSource = backgroundImage;
            }
        }

        private void NavView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
            if (args.IsSettingsSelected)
            {
                mainFrameController.Navigate("settings");
            }
            else if (args.SelectedItemContainer != null)
            {
                string tag = args.SelectedItemContainer.Tag.ToString();
                mainFrameController.Navigate(tag);
            }
        }

        private async Task<string> FetchData(string url)
        {
            HttpResponseMessage httpResponse = await httpClient.GetAsync(url);
            httpResponse.EnsureSuccessStatusCode();
            return await httpResponse.Content.ReadAsStringAsync();
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
                string errorMessage;
                InfoBarSeverity severity = InfoBarSeverity.Error;
                if (ex.Message.Contains("SSL"))
                {
                    errorMessage = "网络连接发生错误\n" + ex.Message;
                    severity = InfoBarSeverity.Warning;
                }
                else
                {
                    errorMessage = ex.Message.Trim() + "\n\n已生成错误报告\n如再次尝试仍会重现错误\n您可以到Github提交Issue";
                }

                ExpectionFileName = string.Format("SRTools_Panic_{0:yyyyMMdd_HHmmss}.WaveToolsPanic", DateTime.Now);

                // 显示InfoBar通知
                AddNotification("严重错误", errorMessage, severity, true, 0, () =>
                {
                    ExpectionFolderOpen_Click();
                }, "打开文件夹");

                Spectre.Console.AnsiConsole.WriteException(ex, Spectre.Console.ExceptionFormats.ShortenPaths | Spectre.Console.ExceptionFormats.ShortenTypes | Spectre.Console.ExceptionFormats.ShortenMethods | Spectre.Console.ExceptionFormats.ShowLinks | Spectre.Console.ExceptionFormats.Default);
                await ExceptionSave.Write("源:" + ex.Source + "\n错误标题:" + ex.Message + "\n堆栈跟踪:\n" + ex.StackTrace + "\n内部异常:\n" + ex.InnerException + "\n结束代码:" + ex.HResult + "\n完整错误:\n" + ex.ToString(), 1, ExpectionFileName);
            }
        }

        private void ExpectionFolderOpen_Click()
        {
            string folderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "JSG-LLC", "Panic");
            Directory.CreateDirectory(folderPath);
            string filePath = Path.Combine(folderPath, ExpectionFileName);
            if (!File.Exists(filePath))
            {
                File.Create(filePath).Dispose();
            }
            Process.Start("explorer.exe", folderPath);
        }

        private DateTime lastNotificationTime = DateTime.MinValue;
        private const int ThrottleTimeMilliseconds = 50;
        public async void AddNotification(string title, string message, InfoBarSeverity severity, bool isClosable = true, int TimerSec = 0, Action actionButtonAction = null, string actionButtonText = null)
        {
            DateTime currentTime = DateTime.Now;
            if ((currentTime - lastNotificationTime).TotalMilliseconds < ThrottleTimeMilliseconds)
            {
                await Task.Delay(ThrottleTimeMilliseconds);
            }
            lastNotificationTime = DateTime.Now;

            if (IsNotificationPresent(message))
            {
                Logging.Write($"Notification with title '{title}' already present, skipping.", 1);
                return;
            }

            Logging.WriteNotification(title, message, (int)severity);
            string titleWithDate = $"{title}";
            InfoBar infoBar = new InfoBar
            {
                Title = titleWithDate,
                Message = message,
                Severity = severity,
                IsOpen = true,
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(0, 0, 0, 8),
                Opacity = 0,
                RenderTransform = new TranslateTransform(),
                IsClosable = isClosable
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
                InfoBarPanel.Children.Remove(infoBar);
            };

            Storyboard moveDownStoryboard = new Storyboard();
            foreach (UIElement child in InfoBarPanel.Children)
            {
                TranslateTransform transform = new TranslateTransform();
                child.RenderTransform = transform;

                DoubleAnimation moveDownAnimation = new DoubleAnimation
                {
                    Duration = new Duration(TimeSpan.FromSeconds(0.2)),
                    From = -20,
                    To = infoBar.ActualHeight,
                    EasingFunction = new BackEase { EasingMode = EasingMode.EaseOut }
                };
                Storyboard.SetTarget(moveDownAnimation, child);
                Storyboard.SetTargetProperty(moveDownAnimation, "(UIElement.RenderTransform).(TranslateTransform.Y)");

                moveDownStoryboard.Children.Add(moveDownAnimation);
            }
            InfoBarPanel.Children.Insert(0, infoBar);
            Storyboard flyInStoryboard = new Storyboard();

            DoubleAnimation translateInAnimation = new DoubleAnimation
            {
                Duration = new Duration(TimeSpan.FromSeconds(0.2)),
                From = 40,
                To = 0,
                EasingFunction = new BackEase { EasingMode = EasingMode.EaseOut }
            };
            Storyboard.SetTarget(translateInAnimation, infoBar);
            Storyboard.SetTargetProperty(translateInAnimation, "(UIElement.RenderTransform).(TranslateTransform.X)");

            DoubleAnimation fadeInAnimation = new DoubleAnimation
            {
                Duration = new Duration(TimeSpan.FromSeconds(0.5)),
                From = 0,
                To = 1,
                EasingFunction = new BackEase { EasingMode = EasingMode.EaseOut }
            };
            Storyboard.SetTarget(fadeInAnimation, infoBar);
            Storyboard.SetTargetProperty(fadeInAnimation, "Opacity");

            moveDownStoryboard.Begin();
            flyInStoryboard.Children.Add(translateInAnimation);
            flyInStoryboard.Children.Add(fadeInAnimation);
            flyInStoryboard.Begin();
            await Task.Delay(10);

            if (TimerSec > 0)
            {
                ProgressBar progressBar = new ProgressBar
                {
                    Width = 300,
                    Height = 2,
                    IsIndeterminate = false,
                    Margin = new Thickness(-48, -4, 0, 0),
                    Maximum = 100,
                    Value = 100,
                    
                };

                if (isClosable)
                {
                    progressBar.Margin = new Thickness(-54, -4, -48, 0);
                }

                infoBar.Content = progressBar;
                var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(TimerSec * 10) };
                timer.Start();
                timer.Tick += async (s, e) =>
                {
                    if (progressBar.Value > 0)
                    {
                        progressBar.Value -= 1;
                    }
                    else
                    {
                        timer.Stop();
                        Storyboard closeStoryboard = new Storyboard();

                        DoubleAnimation translateOutAnimation = new DoubleAnimation
                        {
                            Duration = new Duration(TimeSpan.FromSeconds(0.3)),
                            From = 0,
                            To = 40,
                            EasingFunction = new BackEase { EasingMode = EasingMode.EaseIn }
                        };
                        Storyboard.SetTarget(translateOutAnimation, infoBar);
                        Storyboard.SetTargetProperty(translateOutAnimation, "(UIElement.RenderTransform).(TranslateTransform.X)");

                        DoubleAnimation fadeOutAnimation = new DoubleAnimation
                        {
                            Duration = new Duration(TimeSpan.FromSeconds(0.3)),
                            From = 1,
                            To = 0,
                            EasingFunction = new BackEase { EasingMode = EasingMode.EaseIn }
                        };
                        Storyboard.SetTarget(fadeOutAnimation, infoBar);
                        Storyboard.SetTargetProperty(fadeOutAnimation, "Opacity");
                        closeStoryboard.Children.Add(translateOutAnimation);
                        closeStoryboard.Children.Add(fadeOutAnimation);

                        closeStoryboard.Completed += (s, e) =>
                        {
                            infoBar.IsOpen = false;
                            InfoBarPanel.Children.Remove(infoBar);
                        };

                        closeStoryboard.Begin();
                    }
                };
            }
        }

        public bool IsNotificationPresent(string message)
        {
            foreach (InfoBar infoBar in InfoBarPanel.Children)
            {
                if (infoBar.Message == message)
                {
                    return true;
                }
            }
            return false;
        }


        public async void ShowWaitOverlay(bool status, string title = null, string subtitle = null, bool isProgress = false, int progress = 0, bool isBtnEnabled = false, string btnContent = "", Action btnAction = null)
        {
            if (status)
            {
                if(WaitOverlay.Visibility != Visibility.Visible) FadeInStoryboard.Begin();
                WaitOverlay.Visibility = Visibility.Visible;
                if (isProgress) { WaitOverlay_Progress_Grid.Visibility = Visibility.Visible; WaitOverlay_Progress.Visibility = Visibility.Visible; }
                else WaitOverlay_Progress_Grid.Visibility = Visibility.Collapsed;
                if (progress > 0)
                {
                    WaitOverlay_ProgressBar.Visibility = Visibility.Visible;
                    WaitOverlay_ProgressBar_Value.Visibility = Visibility.Visible;
                    WaitOverlay_ProgressBar.Value = progress;
                    WaitOverlay_ProgressBar_Value.Text = progress.ToString() + "%";
                }
                else
                {
                    WaitOverlay_ProgressBar.Visibility = Visibility.Collapsed;
                    WaitOverlay_ProgressBar_Value.Visibility = Visibility.Collapsed;
                }

                WaitOverlay_Title.Text = title;
                WaitOverlay_SubTitle.Text = subtitle;

                if (isBtnEnabled)
                {
                    WaitOverlay_Button.Visibility = Visibility.Visible;
                    WaitOverlay_Button.IsEnabled = true;
                    buttonAction = btnAction;
                    if (btnContent != "") WaitOverlay_Button.Content = btnContent;
                }
                else
                {
                    WaitOverlay_Button.Visibility = Visibility.Collapsed;
                    WaitOverlay_Button.IsEnabled = false;
                    buttonAction = null;
                }
            }
            else
            {
                if (WaitOverlay.Visibility != Visibility.Collapsed) FadeOutStoryboard.Begin();
                await Task.Delay(100);
                WaitOverlay.Visibility = Visibility.Collapsed;
                WaitOverlay_Progress.Visibility = Visibility.Collapsed;
                WaitOverlay_Success.Visibility = Visibility.Collapsed;
                WaitOverlay_Button.Visibility = Visibility.Collapsed;
                WaitOverlay_Button.IsEnabled = false;
                buttonAction = null;
            }
        }

        private void WaitOverlayButton_Click(object sender, RoutedEventArgs e)
        {
            buttonAction?.Invoke();
        }

        private async void ShowDialog(XamlRoot xamlRoot, string title = null, string content = null, bool isPrimaryButtonEnabled = false, string primaryButtonContent = "", Action primaryButtonAction = null, bool isSecondaryButtonEnabled = false, string secondaryButtonContent = "", Action secondaryButtonAction = null)
        {
            if (isDialogOpen)
            {
                return;
            }

            isDialogOpen = true;

            ContentDialog dialog = new ContentDialog
            {
                XamlRoot = xamlRoot,
                Style = Application.Current.Resources["DefaultContentDialogStyle"] as Style,
                Title = title,
                PrimaryButtonText = isPrimaryButtonEnabled ? primaryButtonContent : null,
                SecondaryButtonText = isSecondaryButtonEnabled ? secondaryButtonContent : null,
                CloseButtonText = "关闭",
                DefaultButton = ContentDialogButton.Primary,
                Content = new TextBlock { Text = content, FontSize = 14 }
            };

            if (isPrimaryButtonEnabled)
            {
                dialog.PrimaryButtonClick += (sender, args) => primaryButtonAction?.Invoke();
            }

            if (isSecondaryButtonEnabled)
            {
                dialog.SecondaryButtonClick += (sender, args) => secondaryButtonAction?.Invoke();
            }

            try
            {
                await dialog.ShowAsync();
            }
            catch (Exception ex)
            {
                NotificationManager.RaiseNotification("对话框出现问题", ex.Message, InfoBarSeverity.Error, true, 5);
            }
            finally
            {
                isDialogOpen = false;
            }
        }

        private void MainWindow_Closed(object sender, WindowEventArgs e)
        {
            NotificationManager.OnNotificationRequested -= AddNotification;
            WaitOverlayManager.OnWaitOverlayRequested -= ShowWaitOverlay;
            DialogManager.OnDialogRequested -= ShowDialog;
        }

    }
}