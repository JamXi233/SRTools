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
using Fiddler;
using WinRT;
using SRTools.Depend;

namespace SRTools
{

    public partial class MainWindow : Window
    {
        private IntPtr hwnd;
        private AppWindow appWindow;
        private AppWindowTitleBar titleBar;
        private SystemBackdrop backdrop;
        private SReg sReg;

        public MainWindow()
        {
            // 设置窗口标题
            this.Title = "JSG-LLC_SRTools";
            this.InitializeComponent();
            this.Activated += OnWindowActivated;

            // 如果注册表项不存在，创建它
            sReg = new SReg();
            sReg.CheckMainReg();
            sReg.CheckReg("SRTools_Config_GamePath", "", true);
            sReg.CheckReg("SRTools_Config_UnlockFPS", "1", true);

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

        private void NavView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
            FiddlerApplication.Shutdown();
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

        private void OnWindowActivated(object sender, WindowActivatedEventArgs e)
        {
            User32.WindowLongFlags GWL_STYLE = User32.WindowLongFlags.GWL_STYLE;
            int WS_THICKFRAME = 0x40000;

            IntPtr hWnd = hwnd;
            int windowStyle = User32.GetWindowLong(hWnd, GWL_STYLE);
            User32.SetWindowLong(hWnd, GWL_STYLE, windowStyle & ~WS_THICKFRAME);
        }

    }
}