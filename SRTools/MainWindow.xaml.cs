// Copyright (c) Microsoft Corporation and Contributors.
// Licensed under the MIT License.

using SRTools.Views;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Scighost.WinUILib.Helpers;
using System;
using System.Runtime.InteropServices;
using Vanara.PInvoke;
using Windows.Graphics;
using Windows.Storage;
using WinRT.Interop;
using Windows.System;
using Microsoft.UI.Xaml.Controls;
using Windows.UI.ApplicationSettings;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.
namespace SRTools
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>

    public partial class MainWindow : Window
    {
        private IntPtr hwnd;

        private AppWindow appWindow;

        private AppWindowTitleBar titleBar;

        private SystemBackdrop backdrop;
        public MainWindow()
        {
            this.InitializeComponent();

            // ������ĸ���ǿ�������
            backdrop = new SystemBackdrop(this);
            backdrop.TrySetMica(fallbackToAcrylic: true);

            // ���ھ��
            hwnd = WindowNative.GetWindowHandle(this);
            WindowId id = Win32Interop.GetWindowIdFromWindow(hwnd);
            appWindow = AppWindow.GetFromWindowId(id);

            // ��ʼ�����ڴ�С��λ��
            appWindow.MoveAndResize(new RectInt32(_X: 560, _Y: 280, _Width: 800, _Height: 500));

            // �Զ��������
            if (AppWindowTitleBar.IsCustomizationSupported())
            {
                // ��֧��ʱ titleBar Ϊ null
                titleBar = appWindow.TitleBar;
                titleBar.ExtendsContentIntoTitleBar = true;
                // ��������������ɫ����Ϊ͸��
                titleBar.ButtonBackgroundColor = Colors.Transparent;
                titleBar.ButtonInactiveBackgroundColor = Colors.Transparent;
                // ��ȡϵͳ������
                var scale = (float)User32.GetDpiForWindow(hwnd) / 96;
                // 48 ���ֵ��Ӧ�ñ������ĸ߶ȣ�����Ψһ�ģ������Լ��� UI ��ƶ���
                titleBar.SetDragRectangles(new RectInt32[] { new RectInt32((int)(48 * scale), 0, 10000, (int)(48 * scale)) });
            }
            else
            {
                ExtendsContentIntoTitleBar = true;
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
            if (args.IsSettingsSelected)
            {
                // Handle settings menu item click
            }
            else if (args.SelectedItemContainer != null)
            {
                switch (args.SelectedItemContainer.Tag.ToString())
                {
                    case "home":
                        // Navigate to Home page
                        MainFrame.Navigate(typeof(MainView));
                        break;
                    case "startgame":
                        // Navigate to Start Game page
                        MainFrame.Navigate(typeof(StartGameView));
                        break;
                }
            }
            if (args.IsSettingsSelected)
            {
                // ��Ĭ�ϵ�����ҳ��
                MainFrame.Navigate(typeof(AboutView));
            }
        }

    }
}
