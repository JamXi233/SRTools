using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using System.Diagnostics;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.Win32;
using SRTools.Depend;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using System.Net.Http;
using System.Text.Json;
using System.Collections.ObjectModel;
using System.Security.Policy;
using Microsoft.UI.Xaml.Input;
using SRTools.Views.NotifyViews;
using Windows.UI.Core;
using System.IO;
using Org.BouncyCastle.Utilities.IO;

namespace SRTools.Views
{
    public sealed partial class MainView : Page
    {
        private static readonly HttpClient httpClient = new HttpClient();
        public ObservableCollection<string> Pictures { get; } = new ObservableCollection<string>();
        public ObservableCollection<string> PicturesClick { get; } = new ObservableCollection<string>();
        private string _url;
        string backgroundUrl = "";
        string iconUrl = "";
        List<String> list = new List<String>();
        GetNotify getNotify = new GetNotify();

        public MainView()
        {
            this.InitializeComponent();
            Logging.Write("Switch to MainView", 0);
            _ = LoadPicturesAsync();
            try 
            { 
                Task.Run(() => getNotify.Get()).Wait();
                Notify_Grid.Visibility = Visibility.Visible;
            } 
            catch 
            { 
                loadRing.Visibility = Visibility.Collapsed;
                loadErr.Visibility = Visibility.Visible;
            }
        }

        private async Task LoadPicturesAsync()
        {
            string apiUrl = "https://api-launcher-static.mihoyo.com/hkrpg_cn/mdk/launcher/api/content?filter_adv=false&key=6KcVuOkbcqjJomjZ&language=zh-cn&launcher_id=33";
            ApiResponse response = await FetchData(apiUrl);
            backgroundUrl = response.data.adv.background;
            iconUrl = response.data.adv.icon;
            _url = response.data.adv.url;
            Logging.Write("LoadPopulatePictures...", 0);
            PopulatePictures(response.data.banner);
            Logging.Write("LoadAdvertisementData...", 0);
            LoadAdvertisementData();
        }

        private async void LoadAdvertisementData()
        {
            // 设置背景图片
            Logging.Write("Getting Background: " + backgroundUrl, 0);
            BitmapImage backgroundImage = new BitmapImage();
            using (var stream = await new HttpClient().GetStreamAsync(backgroundUrl))
            using (var memStream = new MemoryStream())
            {
                await stream.CopyToAsync(memStream);
                memStream.Position = 0;
                var randomAccessStream = memStream.AsRandomAccessStream();
                await backgroundImage.SetSourceAsync(randomAccessStream);
            }
            BackgroundImage.Source = backgroundImage;

            // 设置按钮图标
            Logging.Write("Getting Button Image: " + iconUrl, 0);
            BitmapImage iconImage = new BitmapImage();
            using (var stream = await new HttpClient().GetStreamAsync(iconUrl))
            using (var memStream = new MemoryStream())
            {
                await stream.CopyToAsync(memStream);
                memStream.Position = 0;
                var randomAccessStream = memStream.AsRandomAccessStream();
                await iconImage.SetSourceAsync(randomAccessStream);
            }

            AboutView aboutView = new AboutView();
            int result = await aboutView.OnGetUpdateLatestReleaseInfo("SRToolsHelper", "Depend");
            if (result == 1)
            {
                infoBar.Title = "更新提示";
                infoBar.Message = "依赖包需要更新，请尽快到[设置-检查依赖更新]进行更新";
                infoBar.IsOpen = true;
                infoBar.IsClosable = false;
            }

            IconImageBrush.ImageSource = iconImage;
            loadRing.Visibility = Visibility.Collapsed;
        }

        private void OpenUrlButton_Click(object sender, RoutedEventArgs e)
        {
            // 打开浏览器访问指定URL
            Process.Start(new ProcessStartInfo(_url) { UseShellExecute = true });
        }

        private void BackgroundImage_ImageOpened(object sender, RoutedEventArgs e)
        {
            StartFadeAnimation(BackgroundImage, 0, 1, TimeSpan.FromSeconds(0.2));
            StartFadeAnimation(OpenUrlButton, 0, 1, TimeSpan.FromSeconds(0.2));
        }

        private void StartFadeAnimation(FrameworkElement target, double from, double to, TimeSpan duration)
        {
            DoubleAnimation opacityAnimation = new DoubleAnimation
            {
                From = from,
                To = to,
                Duration = duration,
                EnableDependentAnimation = true
            };
            Storyboard.SetTarget(opacityAnimation, target);
            Storyboard.SetTargetProperty(opacityAnimation, "Opacity");

            Storyboard storyboard = new Storyboard();
            storyboard.Children.Add(opacityAnimation);
            storyboard.Begin();
        }

        public static async Task<ApiResponse> FetchData(string url)
        {
            HttpResponseMessage httpResponse = await httpClient.GetAsync(url);
            Logging.Write("FetchData:"+url, 0);
            httpResponse.EnsureSuccessStatusCode();
            string responseBody = await httpResponse.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<ApiResponse>(responseBody);
        }

        public void PopulatePictures(List<Banner> banners)
        {
            foreach (Banner banner in banners)
            {
                Pictures.Add(banner.img);
                list.Add(banner.url);
            }
            Gallery_Grid.Visibility = Visibility.Visible;
        }

        private void Gallery_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            // 获取当前选中的图片
            int selectedPicture = Gallery.SelectedIndex;

            // 如果选中了图片，则打开浏览器并导航到指定的网页
            string url = list[selectedPicture]; // 替换为要打开的网页地址
            Process.Start(new ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            });
        }

        private void Notify_NavView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
            if (args.IsSettingsSelected)
            {
                // 处理设置菜单项单击事件
            }
            else if (args.SelectedItemContainer != null)
            {
                switch (args.SelectedItemContainer.Tag.ToString())
                {
                    case "Notify_Announce":
                        // 导航到主页
                        NotifyFrame.Navigate(typeof(NotifyAnnounceView));
                        break;
                    case "Notify_Notification":
                        // 导航到启动游戏页
                        NotifyFrame.Navigate(typeof(NotifyNotificationView));
                        break;
                    case "Notify_Message":
                        // 导航到启动游戏页
                        NotifyFrame.Navigate(typeof(NotifyMessageView));
                        break;
                }
            }
        }
    }


    public class ApiResponse
    {
        public int retcode { get; set; }
        public string message { get; set; }
        public Data data { get; set; }
    }

    public class Data
    {
        public Adv adv { get; set; }
        public List<Banner> banner { get; set; }
        public List<Banner> post { get; set; }
    }

    public class Adv
    {
        public string background { get; set; }
        public string icon { get; set; }
        public string url { get; set; }
        public string version { get; set; }
        public string bg_checksum { get; set; }
    }

    public class Banner
    {
        public string banner_id { get; set; }
        public string name { get; set; }
        public string img { get; set; }
        public string url { get; set; }
        public string order { get; set; }
    }

    public class Post
    {
        public string post_id { get; set; }
        public string type { get; set; }
        public string tittle { get; set; }
        public string url { get; set; }
        public string show_time { get; set; }
        public string order { get; set; }
        public string title { get; set; }
    }
}