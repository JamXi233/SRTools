using System;
using Microsoft.UI.Xaml.Controls;
using SRTools.Depend;
using System.Linq;
using Windows.Storage;

namespace SRTools.Views.GachaViews
{
    public sealed partial class LightConeGachaView : Page
    {

        public LightConeGachaView()
        {
            this.InitializeComponent();
            Logging.Write("Switch to LightConeGachaView", 0);
            LoadData();
        }

        private async void LoadData()
        {
            var folder = KnownFolders.DocumentsLibrary;
            var srtoolsFolder = folder.GetFolderAsync("JSG-LLC\\SRTools").AsTask().GetAwaiter().GetResult();
            var settingsFile = srtoolsFolder.GetFileAsync("GachaRecords_LightCone.ini").AsTask().GetAwaiter().GetResult();
            var GachaRecords = FileIO.ReadTextAsync(settingsFile).AsTask().GetAwaiter().GetResult();
            var records = await new GachaRecords().GetAllGachaRecordsAsync(null, GachaRecords);
            var groupedRecords = records.GroupBy(r => r.RankType).ToDictionary(g => g.Key, g => g.ToList());
            int RankType5 = records.TakeWhile(r => r.RankType != "5").Count();
            int RankType4 = records.TakeWhile(r => r.RankType != "4").Count();
            string uid = records.Select(r => r.Uid).FirstOrDefault();

            // 筛选出四星和五星的记录
            var rank4Records = records.Where(r => r.RankType == "4");
            var rank5Records = records.Where(r => r.RankType == "5");

            // 按名称进行分组并计算每个分组中的记录数量
            var rank4Grouped = rank4Records.GroupBy(r => r.Name).Select(g => new { Name = g.Key, Count = g.Count() });
            var rank5Grouped = rank5Records.GroupBy(r => r.Name).Select(g => new { Name = g.Key, Count = g.Count() });

            // 输出五星记录
            var rank5TextBlock = new TextBlock { Text = "五星：\n" };
            var rank4TextBlock = new TextBlock { Text = "四星：\n" };
            foreach (var group in rank5Grouped)
            {
                rank5TextBlock.Text += $"{group.Name} x{group.Count}, \n";
            }
            foreach (var group in rank4Grouped)
            {
                rank4TextBlock.Text += $"{group.Name} x{group.Count}, \n";
            }
            MyStackPanel.Children.Clear();
            MyStackPanel2.Children.Clear();
            MyStackPanel2.Children.Add(rank5TextBlock);
            MyStackPanel2.Children.Add(rank4TextBlock);

            MyStackPanel.Children.Add(new TextBlock { Text = $"UID:" + uid });
            foreach (var group in groupedRecords)
            {
                var textBlock = new TextBlock
                {
                    Text = $"{group.Key}星: {group.Value.Count} (相同的有{group.Value.GroupBy(r => r.Name).Count()}个)"
                };
                MyStackPanel.Children.Add(textBlock);
            }
            MyStackPanel.Children.Add(new TextBlock { Text = $"距离上一个5星已经抽了" + RankType5 + "发" });
            MyStackPanel.Children.Add(new TextBlock { Text = $"距离上一个4星已经抽了" + RankType4 + "发" });
            MyListView.ItemsSource = records;
            //gacha_status.Text = "已加载本地缓存";
        }

    }
}