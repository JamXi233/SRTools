using System;
using Microsoft.UI.Xaml.Controls;
using SRTools.Depend;
using System.Linq;
using Windows.Storage;
using System.Diagnostics;
using System.Collections.Generic;
using System.Text;

namespace SRTools.Views.GachaViews
{
    public sealed partial class CharacterGachaView : Page
    {

        public CharacterGachaView()
        {
            this.InitializeComponent();
            Logging.Write("Switch to CharacterGachaView", 0);
            LoadData();

        }

        private async void LoadData()
        {
            var folder = KnownFolders.DocumentsLibrary;
            var srtoolsFolder = folder.GetFolderAsync("JSG-LLC\\SRTools").AsTask().GetAwaiter().GetResult();
            var settingsFile = srtoolsFolder.GetFileAsync("GachaRecords_Character.ini").AsTask().GetAwaiter().GetResult();
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
            int rank5Count = 0;
            int i;
            int j = 0;
            // 输出五星记录
            var rank5TextBlock = new TextBlock {};
            var rank4TextBlock = new TextBlock {};
            rank5Grouped.Reverse();
            records.Reverse();
            foreach (var group in rank5Grouped)
            {
                for (i=j;i<records.Count;i++)
                {
                    rank5Count++;
                    if (records[i].RankType == "5" && records[i].Name == group.Name)
                    {
                        rank5TextBlock.Text += $"{group.Name} x{group.Count}：用了{rank5Count}抽, \n";
                        rank5Count = 0;
                        j = i+1;
                        break;
                    }
                }
            }
            var lines5 = rank5TextBlock.Text.Split("\n");
            var reversedLines5 = lines5.Reverse();
            rank5TextBlock.Text = string.Join("\n", reversedLines5);

            foreach (var group in rank4Grouped)
            {
                rank4TextBlock.Text += $"{group.Name} x{group.Count}, \n";
            }
            var lines4 = rank4TextBlock.Text.Split("\n");
            var reversedLines4 = lines4.Reverse();
            rank4TextBlock.Text = string.Join("\n", reversedLines4);
            MyStackPanel.Children.Clear();
            Gacha5Stars.Children.Clear();
            Gacha4Stars.Children.Clear();
            Gacha5Stars.Children.Add(rank5TextBlock);
            Gacha4Stars.Children.Add(rank4TextBlock);
            

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