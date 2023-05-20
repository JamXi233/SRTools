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
using Windows.Storage;
using SRTools.Depend;
using System.IO.Compression;
using Windows.Devices.Geolocation;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.
namespace SRTools
{
    /// <summary>
    /// �մ��ڣ�������Ϊ����ʹ�õĴ��ڻ��� Frame �е������ô��ڡ�
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
            // ���ô��ڱ���
            this.Title = "�ǹ칤����";
            this.InitializeComponent();

            _getNetData = new GetNetData();

            // ��ȡ LocalSettings ����
            ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;

            // ָ������ֵ����
            string keyPath = "SRTools";
            string valueFirstRun = "Config_FirstRun";
            string valueGamePath = "Config_Folder";
            string valueUnlockFPS = "Config_UnlockFPS";

            // ָ��Ҫд���ֵ
            string firstRun = "1";
            string folderPath = "Null";
            string unlockFPSValue = "Null";

            // ��ȡ�򴴽�����
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

            // ���ֵ�����ڣ�д����
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

            // ���ֵ�����ڣ�д����
            if (!keyContainer.Values.ContainsKey(valueGamePath))
            {
                keyContainer.Values[valueGamePath] = folderPath;
                Console.WriteLine("Wrote LocalSettings value for SRTools_Config_Folder.");
            }

            // ���ֵ�����ڣ�д����
            if (!keyContainer.Values.ContainsKey(valueUnlockFPS))
            {
                keyContainer.Values[valueUnlockFPS] = unlockFPSValue;
                Console.WriteLine("Wrote LocalSettings value for SRTools_Config_UnlockFPS.");
            }

            // ������ĸ���ǿ�������
            backdrop = new SystemBackdrop(this);
            backdrop.TrySetMica(fallbackToAcrylic: true);

            // ��ȡ���ھ���� appWindow
            hwnd = WindowNative.GetWindowHandle(this);
            WindowId id = Win32Interop.GetWindowIdFromWindow(hwnd);
            appWindow = AppWindow.GetFromWindowId(id);

            // ��ȡϵͳ������
            float scale = (float)User32.GetDpiForWindow(hwnd) / 96;

            // ʹ�������ʵ�������λ�úʹ�С
            int windowX = (int)(560 * scale);
            int windowY = (int)(280 * scale);
            int windowWidth = (int)(1024 * scale);
            int windowHeight = (int)(584 * scale);

            appWindow.MoveAndResize(new RectInt32(windowX, windowY, windowWidth, windowHeight));

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

        private async void DependDownload_Click(object sender, RoutedEventArgs e)
        {
            string userDocumentsFolderPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            string fileUrl = "https://github.com/JamXi233/SRToolsHelper/releases/download/1.0.0.0/SRToolsHelper_1.0.0.0.zip";
            string localFilePath = userDocumentsFolderPath+"\\JSG-LLC\\SRTools\\Depends\\SRToolsHelper.zip";

            // �������ȱ�����
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

                    // ָ��Ҫд���ֵ
                    string firstRun = "1";

                    ZipFile.ExtractToDirectory(userDocumentsFolderPath + "\\JSG-LLC\\SRTools\\Depends\\SRToolsHelper.zip", userDocumentsFolderPath + "\\JSG-LLC\\SRTools\\Depends\\SRToolsHelper\\");
                    update_Grid.Visibility = Visibility.Visible;
                    update_Progress_Grid.Visibility = Visibility.Collapsed;
                    update_Btn_Text.Text = "�������";
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
                    update_Btn_Text.Text = "����ʧ��";
                    update_Btn_Bar.Visibility = Visibility.Collapsed;
                    update_Btn_Icon.Symbol = Symbol.Stop;

                }
            }
            catch (Exception ex) { update_Info.Text = ex.Message; }
        }

        private void ReportProgress(double progressPercentage)
        {
            // ���½�����
            update_Btn_Bar.Value = progressPercentage;
        }

        private void NavView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
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

    }
}