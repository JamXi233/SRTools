using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using System.Diagnostics;
using Microsoft.UI.Xaml.Media.Animation;

namespace SRTools.Views
{
    public sealed partial class MainView : Page
    {
        private string _url;

        public MainView()
        {
            this.InitializeComponent();
            LoadAdvertisementData();
        }

        private void LoadAdvertisementData()
        {
            // 从API获取的数据
            string backgroundUrl = "https://webstatic.mihoyo.com/upload/operation_location/2023/05/13/3a4fc6a540fc834588cb354c9144c739_1612107511066373383.pn";
            string iconUrl = "https://webstatic.mihoyo.com/upload/operation_location/2023/05/13/e955531778044b0cbce4ea084c3389b3_1753505874027884451.png";
            _url = "https://www.miyoushe.com/sr/article/39392377";

            // 设置背景图片
            BitmapImage backgroundImage = new BitmapImage(new Uri(backgroundUrl));
            BackgroundImage.Source = backgroundImage;

            // 设置按钮图标
            BitmapImage iconImage = new BitmapImage(new Uri(iconUrl));
            IconImageBrush.ImageSource = iconImage;
        }

        private void OpenUrlButton_Click(object sender, RoutedEventArgs e)
        {
            // 打开浏览器访问指定URL
            Process.Start(new ProcessStartInfo(_url) { UseShellExecute = true });
        }
        private void BackgroundImage_ImageOpened(object sender, RoutedEventArgs e)
        {
            StartFadeAnimation(BackgroundImage, 0, 1, TimeSpan.FromSeconds(0.1));
            StartFadeAnimation(OpenUrlButton, 0, 1, TimeSpan.FromSeconds(0.1));
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
    }
}