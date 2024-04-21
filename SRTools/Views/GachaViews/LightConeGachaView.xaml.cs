﻿// Copyright (c) 2021-2024, JamXi JSG-LLC.
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
using Microsoft.UI.Xaml.Controls;
using SRTools.Depend;
using System.Linq;
using Windows.Storage;
using SRTools.Views.ToolViews;

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
            string selectedUID = GachaView.GetSelectedUid();
            var folder = KnownFolders.DocumentsLibrary;
            var srtoolsFolder = folder.GetFolderAsync("JSG-LLC\\SRTools\\GachaRecords\\" + selectedUID).AsTask().GetAwaiter().GetResult();
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
            int rank5Count = 0;
            int i;
            int j = 0;
            bool NoEvent = false;

            // 输出五星记录
            var rank5TextBlock = new TextBlock { };
            var rank4TextBlock = new TextBlock { };
            rank5Records.Reverse();
            records.Reverse();
            foreach (var group in rank5Records)
            {
                for (i = j; i < records.Count; i++)
                {
                    //Logging.Write(i+ records[i].Name,0);
                    rank5Count++;
                    if (records[i].RankType == "5" && records[i].Name == group.Name)
                    {
                        Logging.Write("抽到5星:[[" + j + "]]:" + rank5Count + group.Name, 0);
                        if (NoEvent)
                        {
                            rank5TextBlock.Text += $"{group.Name}：用了{rank5Count}抽[大保底] \n";
                            NoEvent = false;
                        }
                        else
                        {
                            rank5TextBlock.Text += $"{group.Name}：用了{rank5Count}抽 \n";

                        }

                        rank5Count = 0;
                        j = i + 1;
                        break; //移动到下一个五星
                    }
                }
            }
            records.Reverse();
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
            // 计算概率
            double upcomingProbability5 = CalculateProbability(RankType5, 80, 0.8, 1.87);
            double upcomingProbability4 = CalculateProbability(RankType4, 10, 6.6, 14.8);


            MyStackPanel.Children.Add(new TextBlock { Text = $"距离上一个五星已经抽了" + RankType5 + "发" });
            MyStackPanel.Children.Add(new TextBlock { Text = $"距离上一个四星已经抽了" + RankType4 + "发" });
            // 显示在 UI 上
            MyStackPanel.Children.Add(new TextBlock { Text = $"下次五星光锥的概率: {upcomingProbability5:F2}%" });
            MyStackPanel.Children.Add(new TextBlock { Text = $"下次四星光锥/角色的概率: {upcomingProbability4:F2}%" });


            MyListView.ItemsSource = records;
            //gacha_status.Text = "已加载本地缓存";
        }


        private double CalculateProbability(int pulls, int maxPulls, double baseRate, double totalRate)
        {
            if (pulls >= maxPulls)
                return 100.0;
            else
            {
                double incrementRate = (totalRate - baseRate) / maxPulls;
                return baseRate + incrementRate * pulls;
            }
        }

    }
}