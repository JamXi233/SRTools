using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using System.Diagnostics;
using Microsoft.UI.Xaml.Media.Animation;
using SRTools.Depend;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Net.Http;
using System.Text.Json;
using System.Collections.ObjectModel;
using Microsoft.UI.Xaml.Input;
using SRTools.Views.NotifyViews;
using System.IO;
using static SRTools.App;
using System.Runtime.InteropServices;

namespace SRTools.Views
{
    public sealed partial class MainView : Page
    {
        private static readonly HttpClient httpClient = new HttpClient();
        public ObservableCollection<string> Pictures { get; } = new ObservableCollection<string>();
        public ObservableCollection<string> PicturesClick { get; } = new ObservableCollection<string>();
        static Dictionary<string, BitmapImage> imageCache = new Dictionary<string, BitmapImage>();

        private string _url;
        string backgroundUrl = "";
        string iconUrl = "";
        GetNotify getNotify = new GetNotify();

        public MainView()
        {
            InitializeComponent();
            Logging.Write("Switch to MainView", 0);
            Loaded += MainView_Loaded;
        }

        private async void MainView_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadPicturesAsync();
            await LoadPostAsync();

            try
            {
                await getNotify.Get();
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
            await LoadGameBackgroundAsync();
            await LoadAdvertisementDataAsync();
        }

        private async Task LoadGameBackgroundAsync()
        {
            string apiUrl = "https://hyp-api.mihoyo.com/hyp/hyp-connect/api/getGames?launcher_id=jGHBHlcOq1&language=zh-cn";
            string responseBody = await FetchData(apiUrl);
            using (JsonDocument doc = JsonDocument.Parse(responseBody))
            {
                JsonElement root = doc.RootElement;
                JsonElement games = root.GetProperty("data").GetProperty("games");

                foreach (JsonElement game in games.EnumerateArray())
                {
                    if (game.GetProperty("biz").GetString() == "hkrpg_cn")
                    {
                        backgroundUrl = game.GetProperty("display").GetProperty("background").GetProperty("url").GetString();
                        break;
                    }
                }
            }

            apiUrl = "https://hyp-api.mihoyo.com/hyp/hyp-connect/api/getAllGameBasicInfo?launcher_id=jGHBHlcOq1&language=zh-cn&game_id=64kMb5iAWu";
            responseBody = await FetchData(apiUrl);
            using (JsonDocument doc = JsonDocument.Parse(responseBody))
            {
                JsonElement root = doc.RootElement;
                JsonElement gameInfoList = root.GetProperty("data").GetProperty("game_info_list");

                foreach (JsonElement gameInfo in gameInfoList.EnumerateArray())
                {
                    JsonElement game = gameInfo.GetProperty("game");
                    if (game.GetProperty("biz").GetString() == "hkrpg_cn")
                    {
                        JsonElement backgrounds = gameInfo.GetProperty("backgrounds")[0];
                        backgroundUrl = backgrounds.GetProperty("background").GetProperty("url").GetString();
                        iconUrl = backgrounds.GetProperty("icon").GetProperty("url").GetString();
                        _url = backgrounds.GetProperty("icon").GetProperty("link").GetString();
                        break;
                    }
                }
            }
        }

        private async Task LoadPostAsync()
        {
            await getNotify.Get();
            NotifyLoad.Visibility = Visibility.Collapsed;
            NotifyNav.Visibility = Visibility.Visible;

            foreach (var menuItem in NotifyNav.Items)
            {
                if (menuItem is SelectorBarItem item && item.IsEnabled)
                {
                    NotifyNav.SelectedItem = item;
                    break;
                }
            }
        }

        private async Task LoadAdvertisementDataAsync()
        {
            Logging.Write("LoadAdvertisementData...", 0);
            Logging.Write("Getting Background: " + backgroundUrl, 0);
            BackgroundImage.Source = await LoadImageAsync(backgroundUrl);

            try
            {
                Logging.Write("Getting Button Image: " + iconUrl, 0);
                IconImageBrush.ImageSource = await LoadImageAsync(iconUrl);
            }
            catch (Exception e)
            {
                Logging.Write("Getting Button Image Error: " + e.Message, 0);
            }

            var result = await GetUpdate.GetDependUpdate();
            var status = result.Status;
            if (status == 1)
            {
                NotificationManager.RaiseNotification("更新提示", "依赖包需要更新\n请尽快到[设置-检查依赖更新]进行更新", InfoBarSeverity.Warning, false, 5);
            }
            result = await GetUpdate.GetSRToolsUpdate();
            status = result.Status;
            if (status == 1)
            {
                NotificationManager.RaiseNotification("更新提示", "SRTools有更新\n可到[设置-检查更新]进行更新", InfoBarSeverity.Warning, false, 5);
            }

            loadRing.Visibility = Visibility.Collapsed;
        }

        private async Task<string> FetchData(string url)
        {
            HttpResponseMessage httpResponse = await httpClient.GetAsync(url);
            httpResponse.EnsureSuccessStatusCode();
            return await httpResponse.Content.ReadAsStringAsync();
        }

        private void BackgroundImage_ImageOpened(object sender, RoutedEventArgs e)
        {
            StartFadeAnimation(BackgroundImage, 0, 1, TimeSpan.FromSeconds(0.2));
            StartFadeAnimation(OpenUrlButton, 0, 1, TimeSpan.FromSeconds(0.2));
        }

        private void OpenUrlButton_Click(object sender, RoutedEventArgs e)
        {
            Process.Start(new ProcessStartInfo(_url) { UseShellExecute = true });
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

        private void Notify_NavView_SelectionChanged(SelectorBar sender, SelectorBarSelectionChangedEventArgs args)
        {
            SelectorBarItem selectedItem = sender.SelectedItem;
            int currentSelectedIndex = sender.Items.IndexOf(selectedItem);
            switch (currentSelectedIndex)
            {
                case 0:
                    NotifyFrame.Navigate(typeof(BannerView));
                    break;
                case 1:
                    NotifyFrame.Navigate(typeof(NotifyAnnounceView));
                    break;
                case 2:
                    NotifyFrame.Navigate(typeof(NotifyNotificationView));
                    break;
                case 3:
                    NotifyFrame.Navigate(typeof(NotifyMessageView));
                    break;
            }
        }

        private async Task<BitmapImage> LoadImageAsync(string imageUrl)
        {
            if (imageCache.ContainsKey(imageUrl))
            {
                return imageCache[imageUrl];
            }

            BitmapImage bitmapImage = new BitmapImage();
            try
            {
                byte[] imageData = await httpClient.GetByteArrayAsync(imageUrl);
                using (var memStream = new MemoryStream(imageData))
                {
                    memStream.Position = 0;
                    bitmapImage = new BitmapImage();
                    await bitmapImage.SetSourceAsync(memStream.AsRandomAccessStream());
                }
            }
            catch (COMException comEx)
            {
                Logging.Write("COMException" + comEx, 2);
            }

            imageCache[imageUrl] = bitmapImage;
            return bitmapImage;
        }
    }
}
