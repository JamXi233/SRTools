// ��Ȩ��Ϣ
// ʹ�� MIT ���֤��
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

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.
namespace SRTools
{
    /// <summary>
    /// �մ��ڣ�������Ϊ����ʹ�õĴ��ڻ��� Frame �е������ô��ڡ�
    /// </summary>
    public partial class MainWindow : Window
    {
        private IntPtr hwnd;
        private AppWindow appWindow;
        private AppWindowTitleBar titleBar;
        private SystemBackdrop backdrop;
        private SReg sReg;

        public MainWindow()
        {
            // ���ô��ڱ���
            this.Title = "JSG-LLC_SRTools";
            this.InitializeComponent();
            this.Activated += OnWindowActivated;

            // ���ע�������ڣ�������
            sReg = new SReg();
            sReg.CheckMainReg();
            sReg.CheckReg("SRTools_Config_GamePath", "", true);
            sReg.CheckReg("SRTools_Config_UnlockFPS", "1", true);

            // ������ĸ���ǿ�������
            backdrop = new SystemBackdrop(this);
            backdrop.TrySetMica(fallbackToAcrylic: true);

            // ��ȡ���ھ���� appWindow
            hwnd = WindowNative.GetWindowHandle(this);
            WindowId id = Win32Interop.GetWindowIdFromWindow(hwnd);
            appWindow = AppWindow.GetFromWindowId(id);

            // ��ʼ�����ڴ�С��λ��
            appWindow.MoveAndResize(new RectInt32(_X: 560, _Y: 280, _Width: 1024, _Height: 584));

            // �Զ��������
            if (AppWindowTitleBar.IsCustomizationSupported())
            {
                // ��֧��ʱ titleBar Ϊ null
                titleBar = appWindow.TitleBar;
                titleBar.ExtendsContentIntoTitleBar = true;
                // ��������������ɫ����Ϊ͸��
                titleBar.ButtonBackgroundColor = Colors.Transparent;
                titleBar.ButtonInactiveBackgroundColor = Colors.Transparent;
                titleBar.ButtonForegroundColor = Colors.White;
                // ��ȡϵͳ������
                var scale = (float)User32.GetDpiForWindow(hwnd) / 96;
                // 48 ���ֵ��Ӧ�ñ������ĸ߶ȣ�����Ψһ�ģ������Լ��� UI ��ƶ���
                titleBar.SetDragRectangles(new RectInt32[] { new RectInt32((int)(48 * scale), 0, 10000, (int)(48 * scale)) });
            }
            else
            {
                // ��չ���ݵ�������
                ExtendsContentIntoTitleBar = true;
                // ���ñ�����
                SetTitleBar(AppTitleBar);
            }
        }

        /// <summary>
        /// RectInt32 �� ulong �໥ת��
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
                // �������ò˵�����¼�
            }
            else if (args.SelectedItemContainer != null)
            {
                switch (args.SelectedItemContainer.Tag.ToString())
                {
                    case "home":
                        // ��������ҳ
                        MainFrame.Navigate(typeof(MainView));
                        break;
                    case "startgame":
                        // ������������Ϸҳ
                        MainFrame.Navigate(typeof(StartGameView));
                        break;
                    case "gacha":
                        // ������������Ϸҳ
                        MainFrame.Navigate(typeof(GachaView));
                        break;
                }
            }
            if (args.IsSettingsSelected)
            {
                // ������Ĭ������ҳ��
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

        public void NavDisable() 
        {
            Nav_Home.IsEnabled = false;
        }

    }
}