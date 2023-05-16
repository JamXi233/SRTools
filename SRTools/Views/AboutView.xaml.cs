using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace SRTools.Views
{
    public sealed partial class AboutView : Page
    {
        // 导入 AllocConsole 和 FreeConsole 函数
        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool AllocConsole();

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool FreeConsole();

        public AboutView()
        {
            Console.WriteLine("123",true);
            InitializeComponent();
        }

        private void Console_Toggle(object sender, RoutedEventArgs e)
        {
            // 判断是否需要打开控制台
            if (consoleToggle.IsChecked ?? false)
            {
                // 调用 AllocConsole 函数以打开控制台
                AllocConsole();
            }
            else
            {
                // 调用 FreeConsole 函数以关闭控制台
                FreeConsole();
            }
        }
    }
}