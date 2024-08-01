// Copyright (c) 2021-2024, JamXi JSG-LLC.
// All rights reserved.

// This file is part of SRTools.

// SRTools is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.

// SRTools is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.

// You should have received a copy of the GNU General Public License
// along with SRTools.  If not, see <http://www.gnu.org/licenses/>.

// For more information, please refer to <https://www.gnu.org/licenses/gpl-3.0.html>

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
using SRTools.Views.NotifyViews;
using System.IO;
using SRTools.Depend;
using SRTools.Views.NotifyViews;
using SRTools;
using System.Runtime.CompilerServices;

namespace SRTools.Views
{
    public sealed partial class MainView : Page
    {
        private static readonly HttpClient httpClient = new HttpClient();
        public ObservableCollection<string> Pictures { get; } = new ObservableCollection<string>();
        public ObservableCollection<string> PicturesClick { get; } = new ObservableCollection<string>();
        static Dictionary<string, BitmapImage> imageCache = new Dictionary<string, BitmapImage>();
        static Dictionary<string, string> imageLinks = new Dictionary<string, string>();

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
            await LoadImageLinksAsync();
            await CompareAndUpdateImageLinks();
            LoadPostAsync();

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

        private async Task LoadGameBackgroundAsync()
        {
            var apiUrl = "https://hyp-api.mihoyo.com/hyp/hyp-connect/api/getAllGameBasicInfo?launcher_id=jGHBHlcOq1&language=zh-cn&game_id=64kMb5iAWu";
            var responseBody = await FetchData(apiUrl);
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

        private async void LoadPostAsync()
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

        private async void LoadAdvertisementDataAsync()
        {
            Logging.Write("LoadAdvertisementData...", 0);

            // 确保文件夹存在
            if (!Directory.Exists(imageFolderPath))
            {
                Directory.CreateDirectory(imageFolderPath);
            }

            await LoadImageAsync("background", backgroundUrl, "background.webp");
            await LoadImageAsync("icon", iconUrl, "icon.png");

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

        private async Task LoadImageAsync(string imageType, string imageUrl, string fileName)
        {
            if (string.IsNullOrEmpty(imageUrl))
            {
                Logging.Write($"{imageType} URL is empty.", 2);
                return;
            }

            string filePath = Path.Combine(imageFolderPath, fileName);
            BitmapImage bitmapImage = new BitmapImage();

            if (imageCache.ContainsKey(imageUrl))
            {
                bitmapImage = imageCache[imageUrl];
            }
            else
            {
                // 尝试加载本地文件
                try
                {
                    if (File.Exists(filePath))
                    {
                        using (var stream = File.OpenRead(filePath))
                        {
                            await bitmapImage.SetSourceAsync(stream.AsRandomAccessStream());
                            imageCache[imageUrl] = bitmapImage;
                            UpdateImageSource(imageType, bitmapImage);
                            return;
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

                    imageCache[imageUrl] = bitmapImage;
                }
                catch (Exception ex)
                {
                    Logging.Write($"Error downloading image from {imageUrl}: {ex.Message}", 2);
                }
            }

            UpdateImageSource(imageType, bitmapImage);
        }

        private void UpdateImageSource(string imageType, BitmapImage bitmapImage)
        {
            if (imageType == "background")
            {
                var mainWindow = (MainWindow)App.MainWindow;
                mainWindow.BackgroundBrush.ImageSource = bitmapImage;
                BackgroundImage.Source = bitmapImage;
            }
            else if (imageType == "icon")
            {
                IconImageBrush.ImageSource = bitmapImage;
            }

            Logging.Write($"{imageType} image loaded successfully", 0);
        }

        private async Task LoadImageLinksAsync()
        {
            if (File.Exists(imageLinksFilePath))
            {
                try
                {
                    string json = await File.ReadAllTextAsync(imageLinksFilePath);
                    imageLinks = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
                    imageLinks.TryGetValue("background", out backgroundUrl);
                    imageLinks.TryGetValue("icon", out iconUrl);
                }
                catch (Exception ex)
                {
                    Logging.Write($"Error loading image links from JSON: {ex.Message}", 2);
                }
            }
        }

        private void SaveImageLinks()
        {
            try
            {
                string json = JsonSerializer.Serialize(imageLinks);
                File.WriteAllText(imageLinksFilePath, json);
            }
            catch (Exception ex)
            {
                Logging.Write($"Error saving image links to JSON: {ex.Message}", 2);
            }
        }

        private async Task CompareAndUpdateImageLinks()
        {
            var apiUrl = "https://hyp-api.mihoyo.com/hyp/hyp-connect/api/getAllGameBasicInfo?launcher_id=jGHBHlcOq1&language=zh-cn&game_id=64kMb5iAWu";
            var responseBody = await FetchData(apiUrl);
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
                        string newBackgroundUrl = backgrounds.GetProperty("background").GetProperty("url").GetString();
                        string newIconUrl = backgrounds.GetProperty("icon").GetProperty("url").GetString();

                        bool isBackgroundUpdated = imageLinks.ContainsKey("background") && imageLinks["background"] != newBackgroundUrl;
                        bool isIconUpdated = imageLinks.ContainsKey("icon") && imageLinks["icon"] != newIconUrl;

                        if (isBackgroundUpdated || isIconUpdated)
                        {
                            imageCache.Clear();
                            DeleteLocalImage("background.webp");
                            DeleteLocalImage("icon.png");
                        }

                        backgroundUrl = newBackgroundUrl;
                        iconUrl = newIconUrl;
                        imageLinks["background"] = backgroundUrl;
                        imageLinks["icon"] = iconUrl;
                        SaveImageLinks();
                        LoadAdvertisementDataAsync();
                        break;
                    }
                }
            }
        }

        private void DeleteLocalImage(string fileName)
        {
            string filePath = Path.Combine(imageFolderPath, fileName);
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }
    }
}
