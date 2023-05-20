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

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.
namespace SRTools
{
    /// <summary>
    /// 空窗口，可以作为单独使用的窗口或在 Frame 中导航到该窗口。
    /// </summary>

    public partial class MainWindow : Window
    {
        private GetNetData _getNetData;
        private IntPtr hwnd;
        private AppWindow appWindow;
        private AppWindowTitleBar titleBar;
        private SystemBackdrop backdrop;

        public MainWindow()
        {
            // 设置窗口标题
            this.Title = "星轨工具箱";
            this.InitializeComponent();

            _getNetData = new GetNetData();

            // 获取 LocalSettings 容器
            ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;

            // 指定键和值名称
            string keyPath = "SRTools";
            string valueFirstRun = "Config_FirstRun";
            string valueGamePath = "Config_Folder";
            string valueUnlockFPS = "Config_UnlockFPS";

            // 指定要写入的值
            string firstRun = "1";
            string folderPath = "Null";
            string unlockFPSValue = "Null";

            // 获取或创建容器
            ApplicationDataContainer keyContainer;
            if (localSettings.Containers.ContainsKey(keyPath))
            {
                keyContainer = localSettings.Containers[keyPath];
            }
            else
            {
                keyContainer = localSettings.CreateContainer(keyPath, ApplicationDataCreateDisposition.Always);
                Console.WriteLine("Created LocalSettings container.");
            }

            // 如果值不存在，写入它
            if (!keyContainer.Values.ContainsKey(valueFirstRun))
            {
                keyContainer.Values[firstRun] = firstRun;
                Console.WriteLine("Wrote LocalSettings value for SRTools_Config_FirstRun.");
                Update.Visibility = Visibility.Collapsed;
                FirstRun.Visibility = Visibility.Visible;
                MainAPP.Visibility = Visibility.Collapsed;
            }
            else if(keyContainer.Values[firstRun].Equals("1"))
            {
                Update.Visibility = Visibility.Collapsed;
                FirstRun.Visibility = Visibility.Collapsed;
                MainAPP.Visibility = Visibility.Visible;
            }

            // 如果值不存在，写入它
            if (!keyContainer.Values.ContainsKey(valueGamePath))
            {
                keyContainer.Values[valueGamePath] = folderPath;
                Console.WriteLine("Wrote LocalSettings value for SRTools_Config_Folder.");
            }

            // 如果值不存在，写入它
            if (!keyContainer.Values.ContainsKey(valueUnlockFPS))
            {
                keyContainer.Values[valueUnlockFPS] = unlockFPSValue;
                Console.WriteLine("Wrote LocalSettings value for SRTools_Config_UnlockFPS.");
            }

            // 设置云母或亚克力背景
            backdrop = new SystemBackdrop(this);
            backdrop.TrySetMica(fallbackToAcrylic: true);

            // 获取窗口句柄和 appWindow
            hwnd = WindowNative.GetWindowHandle(this);
            WindowId id = Win32Interop.GetWindowIdFromWindow(hwnd);
            appWindow = AppWindow.GetFromWindowId(id);

            // 获取系统缩放率
            float scale = (float)User32.GetDpiForWindow(hwnd) / 96;

            // 使用缩放率调整窗口位置和大小
            int windowX = (int)(560 * scale);
            int windowY = (int)(280 * scale);
            int windowWidth = (int)(1024 * scale);
            int windowHeight = (int)(584 * scale);

            appWindow.MoveAndResize(new RectInt32(windowX, windowY, windowWidth, windowHeight));

            // 自定义标题栏
            if (AppWindowTitleBar.IsCustomizationSupported())
            {
                // 不支持时 titleBar 为 null
                titleBar = appWindow.TitleBar;
                titleBar.ExtendsContentIntoTitleBar = true;
                // 标题栏按键背景色设置为透明
                titleBar.ButtonBackgroundColor = Colors.Transparent;
                titleBar.ButtonInactiveBackgroundColor = Colors.Transparent;
                titleBar.ButtonForegroundColor = Colors.White;

                // 48 这个值是应用标题栏的高度，不是唯一的，根据自己的 UI 设计而定
                titleBar.SetDragRectangles(new RectInt32[] { new RectInt32((int)(48 * scale), 0, 10000, (int)(48 * scale)) });
            }
            else
            {
                // 扩展内容到标题栏
                ExtendsContentIntoTitleBar = true;
                // 设置标题栏
                SetTitleBar(AppTitleBar);
            }
        }

            /// <summary>
            /// RectInt32 和 ulong 相互转换
            /// </summary>
            [StructLayout(LayoutKind.Explicit)]
        private struct WindowRect
        {
            [FieldOffset(0)]
            public short X;
            [FieldOffset(2)]
            public short Y;
            [FieldOffset(4)]
            public short Width;
            [FieldOffset(6)]
            public short Height;
            [FieldOffset(0)]
            public ulong Value;

            public int Left => X;
            public int Top => Y;
            public int Right => X + Width;
            public int Bottom => Y + Height;

            public WindowRect(int x, int y, int width, int height)
            {
                X = (short)x;
                Y = (short)y;
                Width = (short)width;
                Height = (short)height;
            }

            public WindowRect(ulong value)
            {
                Value = value;
            }

            public RectInt32 ToRectInt32()
            {
                return new RectInt32(X, Y, Width, Height);
            }
        }

        private async void DependDownload_Click(object sender, RoutedEventArgs e)
        {
            string userDocumentsFolderPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            string fileUrl = "https://github.com/JamXi233/SRToolsHelper/releases/download/1.0.0.0/SRToolsHelper_1.0.0.0.zip";
            string localFilePath = userDocumentsFolderPath+"\\JSG-LLC\\SRTools\\Depends\\SRToolsHelper.zip";

            // 创建进度报告器
            var progress = new Progress<double>(ReportProgress);
            update_Grid.Visibility = Visibility.Collapsed;
            update_Progress_Grid.Visibility = Visibility.Visible;
            try {
                bool downloadResult = await _getNetData.DownloadFileWithProgressAsync(fileUrl, localFilePath, progress); ;
                if (downloadResult)
                {
                    string keyPath = "SRTools";
                    ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
                    ApplicationDataContainer keyContainer;
                    keyContainer = localSettings.Containers[keyPath];
                    string valueFirstRun = "Config_FirstRun";

                    // 指定要写入的值
                    string firstRun = "1";

                    ZipFile.ExtractToDirectory(userDocumentsFolderPath + "\\JSG-LLC\\SRTools\\Depends\\SRToolsHelper.zip", userDocumentsFolderPath + "\\JSG-LLC\\SRTools\\Depends\\SRToolsHelper\\");
                    update_Grid.Visibility = Visibility.Visible;
                    update_Progress_Grid.Visibility = Visibility.Collapsed;
                    update_Btn_Text.Text = "下载完成";
                    update_Btn_Bar.Visibility = Visibility.Collapsed;
                    update_Btn_Icon.Symbol = Symbol.Accept;
                    Update.Visibility = Visibility.Collapsed;
                    FirstRun.Visibility = Visibility.Collapsed;
                    MainAPP.Visibility = Visibility.Visible;
                    keyContainer.Values[firstRun] = "0";
                }
                else
                {
                    update_Grid.Visibility = Visibility.Visible;
                    update_Progress_Grid.Visibility = Visibility.Collapsed;
                    update_Btn_Text.Text = "下载失败";
                    update_Btn_Bar.Visibility = Visibility.Collapsed;
                    update_Btn_Icon.Symbol = Symbol.Stop;

                }
            }
            catch (Exception ex) { update_Info.Text = ex.Message; }
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