using System;
using System.IO;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Threading.Tasks;
using Fiddler;
using Microsoft.UI.Dispatching;
using Windows.ApplicationModel.DataTransfer;
using SRTools.Depend;
using System.Linq;
using Windows.Storage;
using Newtonsoft.Json;
using System.Diagnostics;
using System.Collections.Generic;
using SRTools.Views.GachaViews;
using Spectre.Console;
using Newtonsoft.Json.Linq;

namespace SRTools.Views
{
    public sealed partial class GachaView : Page
    {
        public bool isProxyRunning;
        public String GachaLink_String;
        BCCertMaker.BCCertMaker certProvider = new BCCertMaker.BCCertMaker();
        private DispatcherQueueTimer dispatcherTimer;
        ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
        public GachaView()
        {
            Windows.ApplicationModel.Core.CoreApplication.UnhandledErrorDetected += OnUnhandledErrorDetected;
            var dispatcherQueue = DispatcherQueue.GetForCurrentThread();

            // 创建定时器，并设置回调函数和时间间隔
            dispatcherTimer = dispatcherQueue.CreateTimer();
            dispatcherTimer.Interval = TimeSpan.FromSeconds(2);
            dispatcherTimer.Tick += CheckProcess;
            this.InitializeComponent();
            Logging.Write("Switch to GachaView", 0);
            string uDFP = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            string FileFolder = "\\JSG-LLC\\SRTools\\GachaRecords_Character.ini";
            string FileFolder2 = "\\JSG-LLC\\SRTools\\GachaRecords_LightCone.ini";
            string FileFolder3 = "\\JSG-LLC\\SRTools\\GachaRecords_Newbie.ini";
            string FileFolder4 = "\\JSG-LLC\\SRTools\\GachaRecords_Regular.ini";
            if (localSettings.Values["Gacha_Data"] as string == "1" || File.Exists(uDFP+ FileFolder) && File.Exists(uDFP + FileFolder2) && File.Exists(uDFP + FileFolder3) && File.Exists(uDFP + FileFolder4))
            {
                gachaNav.Visibility = Visibility.Visible;
                gachaFrame.Navigate(typeof(CharacterGachaView));
                localSettings.Values["Gacha_Data"] = "1";
            }


        }

        private FiddlerCoreStartupSettings startupSettings;
        private Proxy oProxy;


        public async Task StartAsync()
        {
            await Task.Run(() =>
            {
                startupSettings = new FiddlerCoreStartupSettingsBuilder()
                    .ListenOnPort(8888)
                    .RegisterAsSystemProxy()
                    .DecryptSSL()
                    .Build();

                FiddlerApplication.BeforeRequest += OnBeforeRequest;
                FiddlerApplication.Startup(startupSettings);
                Logging.Write("Starting Proxy...", 0);
                oProxy = FiddlerApplication.CreateProxyEndpoint(8888, true, "127.0.0.1");
                Logging.Write("Started Proxy", 0);
            });
        }

        public void Stop()
        {
            Logging.Write("Stopping Proxy...", 0);
            FiddlerApplication.Shutdown();
            if (oProxy != null)
            {
                oProxy.Dispose();
            }
        }


        private async void OnBeforeRequest(Session session)
        {
            await Task.Run(() =>
            {
                if (session.fullUrl.Contains("getGachaLog"))
                {
                    GachaLink_String = session.fullUrl;
                }
            });
        }

        private async void ProxyButton_Click(object sender, RoutedEventArgs e)
        {
            if (ProxyButton.IsChecked ?? false)
            {
                if (Process.GetProcessesByName("StarRail").Length > 0)
                {
                    // 进程正在运行
                    try
                    {
                        ProxyButton.IsChecked = true;
                        CertMaker.oCertProvider = certProvider;
                        string folderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "JSG-LLC\\SRTools");
                        string filePath = Path.Combine(folderPath, "RootCertificate.p12");
                        string rootCertificatePassword = "S0m3T0pS3cr3tP4ssw0rd";
                        if (!Directory.Exists(folderPath))
                        {
                            Logging.Write("Creating Proxy Cert...", 0);
                            Directory.CreateDirectory(folderPath);
                        }
                        if (!File.Exists(filePath))
                        {
                            Logging.Write("Creating Proxy Cert...", 0);
                            certProvider.CreateRootCertificate();
                            certProvider.WriteRootCertificateAndPrivateKeyToPkcs12File(filePath, rootCertificatePassword);
                        }
                        certProvider.ReadRootCertificateAndPrivateKeyFromPkcs12File(filePath, rootCertificatePassword);
                        if (!CertMaker.rootCertIsTrusted())
                        {
                            Logging.Write("Trust Proxy Cert...", 0);
                            CertMaker.trustRootCert();
                        }
                        isProxyRunning = true;
                        await StartAsync();
                        gacha_status.Text = "正在等待打开抽卡历史记录...";
                        ProxyButton.IsEnabled = true;
                        dispatcherTimer.Start();
                    }
                    catch (Exception ex)
                    {
                        GachaLink.Subtitle = ex.Message;
                        GachaLink.IsOpen = true;
                        AnsiConsole.WriteException(ex);
                    }
                }
                else
                {
                    // 进程未运行
                    ProxyButton.IsChecked = false;
                    GachaLink.Title = "未运行游戏";
                    GachaLink.Subtitle = "先到启动游戏页面启动游戏后再启动代理";
                    GachaLink.IsOpen = true;
                }
                
            }
            else { Stop(); isProxyRunning = false; }
        }
        // 定时器回调函数，检查进程是否正在运行
        private void CheckProcess(DispatcherQueueTimer timer, object e)
        {
            if (isProxyRunning)
            {
                ProxyButton.IsChecked = true;
                // 进程正在运行
                if (GachaLink_String != null && GachaLink_String.Contains("getGachaLog"))
                {
                    gacha_status.Text = GachaLink_String;
                    GachaLink.Title = "获取到抽卡记录地址";
                    GachaLink.Subtitle = "正在获取API信息,请不要退出...";
                    GachaLink.IsOpen = true;
                    ProxyButton.IsEnabled = false;
                    Stop();
                    Logging.Write("Get GachaLink Finish!", 0);
                    ProxyButton.IsChecked = false;
                    DataPackage dataPackage = new DataPackage();
                    dataPackage.SetText(GachaLink_String);
                    Logging.Write("Writing GachaLink in Clipboard...", 0);
                    Clipboard.SetContent(dataPackage);
                    dispatcherTimer.Stop();
                    Logging.Write("Loading Gacha Data...", 0);
                    gacha_status.Text = "正在获取API信息,请不要退出...";
                    LoadDataAsync(GachaLink_String);
                    GachaLink_String = null;
                }
            }
            else
            {
                ProxyButton.IsChecked = false;
            }
        }

        private async void LoadDataAsync(String url)
        {
            var char_records = await new GachaRecords().GetAllGachaRecordsAsync(url, null,"11");
            var light_records = await new GachaRecords().GetAllGachaRecordsAsync(url, null,"12");
            var newbie_records = await new GachaRecords().GetAllGachaRecordsAsync(url, null, "2");
            var regular_records = await new GachaRecords().GetAllGachaRecordsAsync(url, null, "1");
            var folder = KnownFolders.DocumentsLibrary;
            var srtoolsFolder = await folder.CreateFolderAsync("JSG-LLC\\SRTools", CreationCollisionOption.OpenIfExists);
            var char_gachaFile = await srtoolsFolder.CreateFileAsync("GachaRecords_Character.ini", CreationCollisionOption.OpenIfExists);
            var light_gachaFile = await srtoolsFolder.CreateFileAsync("GachaRecords_LightCone.ini", CreationCollisionOption.OpenIfExists);
            var newbie_gachaFile = await srtoolsFolder.CreateFileAsync("GachaRecords_Newbie.ini", CreationCollisionOption.OpenIfExists);
            var regular_gachaFile = await srtoolsFolder.CreateFileAsync("GachaRecords_Regular.ini", CreationCollisionOption.OpenIfExists);
            var char_serializedList = JsonConvert.SerializeObject(char_records);//获取到的数据
            var light_serializedList = JsonConvert.SerializeObject(light_records);//获取到的数据
            var newbie_serializedList = JsonConvert.SerializeObject(newbie_records);//获取到的数据
            var regular_serializedList = JsonConvert.SerializeObject(regular_records);//获取到的数据
            Logging.Write("正在获取API信息,请不要退出... | 正在获取角色池...", 0);
            DataChange(char_serializedList,char_gachaFile);
            Logging.Write("正在获取API信息,请不要退出... | 正在获取光锥池...", 0);
            DataChange(light_serializedList,light_gachaFile);
            Logging.Write("正在获取API信息,请不要退出... | 正在获取新手池...", 0);
            DataChange(newbie_serializedList,newbie_gachaFile);
            Logging.Write("正在获取API信息,请不要退出... | 正在获取常驻池...", 0);
            DataChange(regular_serializedList,regular_gachaFile);


        }

        private async void DataChange(String serializedList, StorageFile gachaFile) 
        {
            
            var GachaRecords = FileIO.ReadTextAsync(gachaFile).AsTask().GetAwaiter().GetResult();//原来的数据

            if (GachaRecords != "") //如果不为空
            {
                // 反序列化为List<Record>对象
                List<GachaRecords> data1 = JsonConvert.DeserializeObject<List<GachaRecords>>(serializedList);
                List<GachaRecords> data2 = JsonConvert.DeserializeObject<List<GachaRecords>>(GachaRecords);
                // 合并数据，确保ID不重复
                var combinedData = data1.Concat(data2)
                            .GroupBy(r => r.Id)
                            .Select(g => g.First())
                            .ToList();
                string combinedDataJson = JsonConvert.SerializeObject(combinedData);
                // 如果需要，将合并后的数据序列化为JSON字符串
                // 消除ID为空的记录
                JArray data = JArray.Parse(combinedDataJson);
                for (int i = data.Count - 1; i >= 0; i--)
                {
                    if (data[i]["Id"].ToString() == "")
                    {
                        data.RemoveAt(i);
                    }
                }
                combinedDataJson = JsonConvert.SerializeObject(data);
                await FileIO.WriteTextAsync(gachaFile, combinedDataJson);
            }
            else
            {
                await FileIO.WriteTextAsync(gachaFile, serializedList);
            }
            localSettings.Values["Gacha_Data"] = "1";
            GachaLink.IsOpen = false;
            gacha_status.Text = "API获取完成";
            gachaNav.Visibility = Visibility.Visible;
            gachaFrame.Navigate(typeof(CharacterGachaView));
            ProxyButton.IsEnabled = true;
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
                    case "CharacterGacha":
                        // 导航到主页
                        gachaFrame.Navigate(typeof(CharacterGachaView));
                        break;
                    case "LightConeGacha":
                        // 导航到启动游戏页
                        gachaFrame.Navigate(typeof(LightConeGachaView));
                        break;
                    case "RegularGacha":
                        // 导航到启动游戏页
                        gachaFrame.Navigate(typeof(RegularGachaView));
                        break;
                    case "NewbieGacha":
                        // 导航到启动游戏页
                        gachaFrame.Navigate(typeof(NewbieGachaView));
                        break;
                }
            }
        }

        private void OnUnhandledErrorDetected(object sender, Windows.ApplicationModel.Core.UnhandledErrorDetectedEventArgs e)
        {
            try
            {
                e.UnhandledError.Propagate();
            }
            catch (Exception ex)
            {
                infoBar.IsOpen = true;
                infoBar.Title = "灾难性错误";
                infoBar.Message = ex.Message;
            }
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            dispatcherTimer.Stop();
            dispatcherTimer.Tick -= CheckProcess;
        }

        
    }
}