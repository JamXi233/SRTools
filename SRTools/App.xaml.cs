﻿// Copyright (c) Microsoft Corporation and Contributors.
// Licensed under the MIT License.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using SRTools.Depend;
using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Windows.Storage;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace SRTools
{
    public partial class App : Application
    {
        public static bool SDebugMode { get; set; }
        // 导入 AllocConsole 和 FreeConsole 函数
        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool AllocConsole();
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool FreeConsole();
        // 导入 GetAsyncKeyState 函数
        [DllImport("User32.dll")]
        public static extern short GetAsyncKeyState(int vKey);
        GetNotify getNotify = new GetNotify();
        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            this.InitializeComponent();
        }

        /// <summary>
        /// Invoked when the application is launched.
        /// </summary>
        /// <param name="args">Details about the launch request and process.</param>
        protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            // 获取根Frame并导航到初始页面
            Init();

            bool isShiftPressed = (GetAsyncKeyState(0x10) & 0x8000) != 0;

            if (isShiftPressed)
            {
                Logging.Write("Pressed", 1);
                Console.Title = "🆂𝐃𝐞𝐛𝐮𝐠𝐌𝐨𝐝𝐞:SRTools";
                TerminalMode.ShowConsole();
                SDebugMode = true;
            }
            else
            {
                Logging.Write("NoPressed", 1);
            }
        }

        public async void Init()
        {
            AllocConsole();
            Console.SetWindowSize(60, 25);
            Console.SetBufferSize(60, 25);
            bool isDebug = false;
            #if DEBUG
            isDebug = true;
            #else
            #endif

            if (isDebug)
            {
                Logging.Write("Debug Mode", 1);
            }
            else
            {
                Logging.Write("Release Mode", 1);
            }
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
            try { Task.Run(() => getNotify.Get()).Wait(); } catch { }
            if (localSettings.Values["Config_TerminalMode"] != null)
            {
                int Mode = (int)localSettings.Values["Config_TerminalMode"];
                TerminalMode terminalMode = new TerminalMode();
                bool response = await terminalMode.Init(Mode);
                if (response)
                {
                    m_window = new MainWindow();
                    m_window.Activate();
                }
            }
            else
            {
                m_window = new MainWindow();
                m_window.Activate();
            }
            if (isDebug)
            {
                Console.Title = "𝐃𝐞𝐛𝐮𝐠𝐌𝐨𝐝𝐞:SRTools";
                TerminalMode.ShowConsole();
            }

        }
        private Window m_window;
    }
}
