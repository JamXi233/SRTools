// Copyright (c) Microsoft Corporation and Contributors.
// Licensed under the MIT License.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI.Xaml.Shapes;
using SRTools.Depend;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.UI.Popups;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace SRTools
{
    public partial class App : Application
    {
        // 导入 AllocConsole 和 FreeConsole 函数
        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool AllocConsole();
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool FreeConsole();
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
            Init();
        }

        public async void Init()
        {
            AllocConsole();
            Console.SetWindowSize(50, 20);
            Console.SetBufferSize(50, 20);
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
            Task.Run(() => getNotify.Get()).Wait();
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


        private async void OnUnhandledErrorDetected(object sender, Windows.ApplicationModel.Core.UnhandledErrorDetectedEventArgs e)
        {
            try
            {
                e.UnhandledError.Propagate();
            }
            catch (Exception ex)
            {
                var m_window = Window.Current;
                try { m_window.Close(); }catch { }
                TerminalMode.ShowConsole();
                TerminalMode terminalMode = new TerminalMode();
                terminalMode.Init(0, 1, ex.Message);
                // 关闭应用程序
                //await Task.Delay(TimeSpan.FromSeconds(5));
                //Application.Current.Exit();
            }
        }
        private Window m_window;
    }
}
