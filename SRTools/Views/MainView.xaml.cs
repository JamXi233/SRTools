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

        private string imageFolderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "JSG-LLC", "SRTools", "images");
        private string imageLinksFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "JSG-LLC", "SRTools", "images", "imagelinks.json");


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

            // 确保文件夹存在
            if (!Directory.Exists(imageFolderPath))
            {
                Directory.CreateDirectory(imageFolderPath);
            }

            Logging.Write("Getting Background: " + backgroundUrl, 0);

            try
            {
                bool isBackgroundUpdated = await IsImageLinkUpdatedAsync("background", backgroundUrl);
                if (!isBackgroundUpdated)
                {
                    BackgroundImage.Source = await LoadImageAsync(backgroundUrl, "background.jpg");
                    Logging.Write("Background image loaded from local file", 0);
                }
                else
                {
                    BackgroundImage.Source = await LoadImageAsync(backgroundUrl, "background.jpg");
                    Logging.Write("Background image updated and loaded successfully", 0);
                }
            }
            catch (Exception ex)
            {
                Logging.Write($"Error loading background image: {ex.Message}", 2);
            }

            try
            {
                Logging.Write("Getting Button Image: " + iconUrl, 0);
                bool isIconUpdated = await IsImageLinkUpdatedAsync("icon", iconUrl);
                if (!isIconUpdated)
                {
                    IconImageBrush.ImageSource = await LoadImageAsync(iconUrl, "icon.jpg");
                    Logging.Write("Button image loaded from local file", 0);
                }
                else
                {
                    IconImageBrush.ImageSource = await LoadImageAsync(iconUrl, "icon.jpg");
                    Logging.Write("Button image updated and loaded successfully", 0);
                }
            }
            catch (Exception e)
            {
                Logging.Write("Getting Button Image Error: " + e.Message, 0);
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

        private async Task<BitmapImage> LoadImageAsync(string imageUrl, string fileName)
        {
            string filePath = Path.Combine(imageFolderPath, fileName);
            BitmapImage bitmapImage = new BitmapImage();

            // 尝试加载本地文件
            try
            {
                if (File.Exists(filePath))
                {
                    using (var stream = File.OpenRead(filePath))
                    {
                        await bitmapImage.SetSourceAsync(stream.AsRandomAccessStream());
                        imageCache[imageUrl] = bitmapImage;
                        return bitmapImage;
                    }
                }
                else
                {
                    Logging.Write($"Local file not found: {filePath}", 2);
                }
            }
            catch (Exception ex)
            {
                Logging.Write($"Error loading local image from {filePath}: {ex.Message}", 2);
            }

            // 本地文件加载失败或不存在，尝试下载并保存图片
            try
            {
                byte[] imageData = await httpClient.GetByteArrayAsync(imageUrl);
                await File.WriteAllBytesAsync(filePath, imageData);

                using (var memStream = new MemoryStream(imageData))
                {
                    memStream.Position = 0;
                    await bitmapImage.SetSourceAsync(memStream.AsRandomAccessStream());
                }

                SaveImageLink(fileName, imageUrl);
                imageCache[imageUrl] = bitmapImage;
            }
            catch (Exception ex)
            {
                Logging.Write($"Error downloading image from {imageUrl}: {ex.Message}", 2);
            }

            return bitmapImage;
        }





        private async Task<bool> IsImageLinkUpdatedAsync(string imageType, string newUrl)
        {
            Dictionary<string, string> imageLinks = new Dictionary<string, string>();

            if (File.Exists(imageLinksFilePath))
            {
                string json = await File.ReadAllTextAsync(imageLinksFilePath);
                imageLinks = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
            }

            if (imageLinks.ContainsKey(imageType) && imageLinks[imageType] == newUrl)
            {
                return false;
            }

            imageLinks[imageType] = newUrl;
            string updatedJson = JsonSerializer.Serialize(imageLinks);
            await File.WriteAllTextAsync(imageLinksFilePath, updatedJson);

            return true;
        }



        private void SaveImageLink(string imageType, string newUrl)
        {
            Dictionary<string, string> imageLinks = new Dictionary<string, string>();

            if (File.Exists(imageLinksFilePath))
            {
                string json = File.ReadAllText(imageLinksFilePath);
                imageLinks = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
            }

            imageLinks[imageType] = newUrl;
            string updatedJson = JsonSerializer.Serialize(imageLinks);
            File.WriteAllText(imageLinksFilePath, updatedJson);
        }
    }
}
