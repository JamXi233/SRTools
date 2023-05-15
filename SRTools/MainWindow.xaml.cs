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

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.
namespace SRTools
{
    /// <summary>
    /// 空窗口，可以作为单独使用的窗口或在 Frame 中导航到该窗口。
    /// </summary>
    public partial class MainWindow : Window
    {
        private IntPtr hwnd;
        private AppWindow appWindow;
        private AppWindowTitleBar titleBar;
        private SystemBackdrop backdrop;

        public MainWindow()
        {
            // 设置窗口标题
            this.Title = "JSG-LLC_SRTools";
            this.InitializeComponent();

            //检查注册表项
            string keyPath = @"Software\miHoYo\崩坏：星穹铁道";
            string valueGamePath = "SRTools_Config_GamePath";
            string valueUnlockFPS = "SRTools_Config_UnlockFPS";
            string folderPath = @"Null";
            int unlockFPSValue = 1;

            // 打开注册表项
            RegistryKey baseKey = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Registry32);
            RegistryKey key = baseKey.OpenSubKey(keyPath, true);

            // 如果注册表项不存在，创建它
            if (key == null)
            {
                key = baseKey.CreateSubKey(keyPath);
                Console.WriteLine("Created registry key.");
            }

            // 如果值不存在，写入它
            if (key.GetValue(valueGamePath) == null)
            {
                key.SetValue(valueGamePath, folderPath, RegistryValueKind.String);
                Console.WriteLine("Wrote registry value for SRTools_Config_Folder.");
            }

            // 如果值不存在，写入它
            if (key.GetValue(valueUnlockFPS) == null)
            {
                key.SetValue(valueUnlockFPS, unlockFPSValue, RegistryValueKind.String);
                Console.WriteLine("Wrote registry value for SRTools_Config_UnlockFPS.");
            }

            // 关闭注册表项
            key.Close();

            // 设置云母或亚克力背景
            backdrop = new SystemBackdrop(this);
            backdrop.TrySetMica(fallbackToAcrylic: true);

            // 获取窗口句柄和 appWindow
            hwnd = WindowNative.GetWindowHandle(this);
            WindowId id = Win32Interop.GetWindowIdFromWindow(hwnd);
            appWindow = AppWindow.GetFromWindowId(id);

            // 初始化窗口大小和位置
            appWindow.MoveAndResize(new RectInt32(_X: 560, _Y: 280, _Width: 800, _Height: 500));

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
                // 获取系统缩放率
                var scale = (float)User32.GetDpiForWindow(hwnd) / 96;
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