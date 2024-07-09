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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.UI;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using Newtonsoft.Json;
using SRTools.Depend;
using SRTools.Views.ToolViews;

namespace SRTools.Views.GachaViews
{
    public sealed partial class TempGachaView : Page
    {
        private CountToProgressWidthConverter progressWidthConverter;
        public TempGachaView()
        {
            this.InitializeComponent();
            Logging.Write("Switch to TempGachaView", 0);
            progressWidthConverter = new CountToProgressWidthConverter();
            LoadData();
        }

        private async void LoadData()
        {
            Logging.Write("Starting LoadData method", 0);
            string selectedUID = GachaView.selectedUid;
            int selectedCardPoolId = GachaView.selectedCardPoolId;
            Logging.Write($"Selected UID: {selectedUID}, Selected Card Pool ID: {selectedCardPoolId}", 0);

            string recordsDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "JSG-LLC", "SRTools", "GachaRecords");
            string filePath = Path.Combine(recordsDirectory, $"{selectedUID}.json");
            Logging.Write($"Records Directory: {recordsDirectory}, File Path: {filePath}", 0);

            if (!File.Exists(filePath))
            {
                Logging.Write("File not found: " + filePath, 1);
                Console.WriteLine("找不到UID的跃迁记录文件");
                return;
            }

            Logging.Write("Reading file content", 0);
            string jsonContent = await File.ReadAllTextAsync(filePath);
            Logging.Write("Deserializing JSON content", 0);
            var gachaData = JsonConvert.DeserializeObject<GachaModel.GachaData>(jsonContent);
            var records = gachaData.list.Where(pool => pool.cardPoolId == selectedCardPoolId).SelectMany(pool => pool.records).ToList();
            Logging.Write($"Total records found: {records.Count}", 0);

            // 获取selectedCardPool
            var selectedCardPool = GachaView.cardPoolInfo.CardPools.FirstOrDefault(cp => cp.CardPoolId == selectedCardPoolId);

            // 筛选出四星和五星的记录
            var rank4Records = records.Where(r => r.rankType == "4").ToList();
            var rank5Records = records.Where(r => r.rankType == "5").ToList();
            Logging.Write($"4-star records count: {rank4Records.Count}, 5-star records count: {rank5Records.Count}", 0);

            // 按名称进行分组并计算每个分组中的记录数量
            var rank4Grouped = rank4Records.GroupBy(r => r.name).Select(g => new GachaModel.GroupedRecord { name = g.Key, count = g.Count() }).ToList();
            var rank5Grouped = rank5Records.GroupBy(r => r.name).Select(g => new GachaModel.GroupedRecord { name = g.Key, count = g.Count() }).ToList();
            Logging.Write("Grouped records by name", 0);

            // 显示记录详情
            DisplayGachaDetails(gachaData, rank4Records, rank5Records, selectedCardPoolId, GachaView.cardPoolInfo);

            // 显示抽卡详情
            DisplayGachaInfo(records, selectedCardPoolId);

            // 显示跃迁记录
            DisplayGachaRecords(records);

            Logging.Write("LoadData method finished", 0);
        }

        private void DisplayGachaRecords(List<GachaModel.GachaRecord> records)
        {
            Logging.Write("Displaying gacha records", 0);
            GachaRecords_List.ItemsSource = records;
        }

        private void DisplayGachaInfo(List<GachaModel.GachaRecord> records, int selectedCardPoolId)
        {
            Logging.Write("Displaying gacha info", 0);
            var selectedCardPool = GachaView.cardPoolInfo.CardPools.FirstOrDefault(cp => cp.CardPoolId == selectedCardPoolId);

            var rank5Records = records.Where(r => r.rankType == "5")
                                       .Select(r => new
                                       {
                                           r.name,
                                           Count = CalculateCount(records, r.id, "5"),
                                           Pity = CalculatePity(records, r.name, "5", selectedCardPoolId, GachaView.cardPoolInfo),
                                           PityVisibility = (bool)selectedCardPool.isPityEnable ? Visibility.Collapsed : Visibility.Collapsed
                                       }).ToList();

            if (rank5Records.Count == 0) GachaInfo_List_Disable.Visibility = Visibility.Visible;

            GachaInfo_List.ItemsSource = rank5Records;
            Logging.Write("Finished displaying gacha info", 0);
        }

        private string CalculateCount(List<GachaModel.GachaRecord> records, string id, string rankType)
        {
            Logging.Write("Calculating count since last target star", 0);
            int countSinceLastTargetStar = 1;
            bool foundTargetStar = false;
            for (int i = records.Count - 1; i >= 0; i--)
            {
                var record = records[i];
                if (record.rankType == rankType && record.id == id)
                {
                    foundTargetStar = true;
                    break;
                }
                if (record.rankType == "5")
                {
                    countSinceLastTargetStar = 1;
                }
                else
                {
                    countSinceLastTargetStar++;
                }
            }
            if (!foundTargetStar)
            {
                return "未找到";
            }

            Logging.Write($"Count since last target star: {countSinceLastTargetStar}", 0);
            return $"{countSinceLastTargetStar}";
        }


        private string CalculatePity(List<GachaModel.GachaRecord> records, string name, string rankType, int selectedCardPoolId, GachaModel.CardPoolInfo cardPoolInfo)
        {
            Logging.Write("Calculating pity", 0);
            var selectedCardPool = cardPoolInfo.CardPools.FirstOrDefault(cp => cp.CardPoolId == selectedCardPoolId);
            var specialNames = new List<string> { "猫又", "「11号」", "珂蕾妲", "莱卡恩", "格莉丝", "丽娜" };

            if (specialNames.Contains(name))
            {
                if ((bool)!selectedCardPool.isPityEnable) return "";
                Logging.Write("Pity result: 歪了", 0);
                return "歪了";
            }
            else
            {
                Logging.Write("Pity result: 没歪", 0);
                return "";
            }
        }

        private List<int> CalculateIntervals(List<GachaModel.GachaRecord> records, string rankType)
        {
            var intervals = new List<int>();
            int countSinceLastStar = 0;

            // 倒序遍历记录
            foreach (var record in records.AsEnumerable().Reverse())
            {
                countSinceLastStar++; // 每次迭代都递增计数器

                if (record.rankType == rankType)
                {
                    intervals.Add(countSinceLastStar); // 将计数器的值添加到间隔列表中
                    countSinceLastStar = 0; // 重置计数器
                }
            }

            return intervals;
        }

        private void DisplayGachaDetails(GachaModel.GachaData gachaData, List<GachaModel.GachaRecord> rank4Records, List<GachaModel.GachaRecord> rank5Records, int selectedCardPoolId, GachaModel.CardPoolInfo cardPoolInfo)
        {
            Logging.Write("Displaying gacha details", 0);
            Gacha_Panel.Children.Clear();
            var scrollView = new ScrollViewer
            {
                HorizontalScrollBarVisibility = ScrollBarVisibility.Hidden,
                VerticalScrollBarVisibility = ScrollBarVisibility.Hidden,
                Height = 320
            };

            var contentPanel = new StackPanel();

            var selectedRecords = gachaData.list
                .Where(pool => pool.cardPoolId == selectedCardPoolId)
                .SelectMany(pool => pool.records)
                .OrderByDescending(r => r.time)
                .ToList();

            Logging.Write($"Total selected records: {selectedRecords.Count}", 0);

            int countSinceLast5Star = 0;
            int countSinceLast4Star = 0;
            bool foundLast5Star = false;
            bool foundLast4Star = false;

            foreach (var record in selectedRecords)
            {
                if (!foundLast5Star && record.rankType == "5")
                {
                    foundLast5Star = true;
                    foundLast4Star = true;
                }
                else if (!foundLast5Star)
                {
                    countSinceLast5Star++;
                }

                if (!foundLast4Star && record.rankType == "4")
                {
                    foundLast4Star = true;
                }
                else if (!foundLast4Star)
                {
                    countSinceLast4Star++;
                }

                if (foundLast5Star && foundLast4Star)
                {
                    break;
                }
            }


            // 计算四星和五星的间隔
            var fourStarIntervals = CalculateIntervals(selectedRecords, "4");
            var fiveStarIntervals = CalculateIntervals(selectedRecords, "5");

            // 计算平均值
            string averageDraws4Star = fourStarIntervals.Count > 0 ? (fourStarIntervals.Average()).ToString("F2") : "∞";
            string averageDraws5Star = fiveStarIntervals.Count > 0 ? (fiveStarIntervals.Average()).ToString("F2") : "∞";

            Gacha_UID.Text = gachaData.info.uid;
            GachaRecords_Count.Text = "共" + selectedRecords.Count() + "抽";
            GachaInfo_SinceLast5Star.Text = $"垫了{countSinceLast5Star}发";

            var basicInfoPanel = CreateDetailBorder();
            var stackPanelBasicInfo = new StackPanel();
            stackPanelBasicInfo.Children.Add(new TextBlock { Text = $"UID: {gachaData.info.uid}", FontWeight = FontWeights.Bold });
            stackPanelBasicInfo.Children.Add(new TextBlock { Text = $"总计抽数: {selectedRecords.Count}" });
            stackPanelBasicInfo.Children.Add(new TextBlock { Text = $"抽到S级次数: {rank5Records.Count}" });
            stackPanelBasicInfo.Children.Add(new TextBlock { Text = $"抽到A级次数: {rank4Records.Count}" });
            stackPanelBasicInfo.Children.Add(new TextBlock { Text = $"预计使用菲林: {selectedRecords.Count * 160}" });
            basicInfoPanel.Child = stackPanelBasicInfo;
            contentPanel.Children.Add(basicInfoPanel);

            var detailInfoPanel = CreateDetailBorder();
            var stackPanelDetailInfo = new StackPanel();
            stackPanelDetailInfo.Children.Add(new TextBlock { Text = "详细统计", FontWeight = FontWeights.Bold });

            stackPanelDetailInfo.Children.Add(new TextBlock { Text = $"S级平均抽数: {averageDraws5Star}抽" });
            stackPanelDetailInfo.Children.Add(new TextBlock { Text = $"A级平均抽数: {averageDraws4Star}抽" });

            string rate4Star = rank4Records.Count > 0 ? (rank4Records.Count / (double)selectedRecords.Count * 100).ToString("F2") + "%" : "∞";
            string rate5Star = rank5Records.Count > 0 ? (rank5Records.Count / (double)selectedRecords.Count * 100).ToString("F2") + "%" : "∞";

            stackPanelDetailInfo.Children.Add(new TextBlock { Text = $"S级获取率: {rate5Star}" });
            stackPanelDetailInfo.Children.Add(new TextBlock { Text = $"A级获取率: {rate4Star}" });

            if (rank5Records.Any())
            {
                stackPanelDetailInfo.Children.Add(new TextBlock { Text = $"最近S级: {rank5Records.First().time}" });
            }
            else
            {
                stackPanelDetailInfo.Children.Add(new TextBlock { Text = "最近S级: ∞" });
            }

            if (rank4Records.Any())
            {
                stackPanelDetailInfo.Children.Add(new TextBlock { Text = $"最近A级: {rank4Records.First().time}" });
            }
            else
            {
                stackPanelDetailInfo.Children.Add(new TextBlock { Text = "最近A级: ∞" });
            }

            detailInfoPanel.Child = stackPanelDetailInfo;
            contentPanel.Children.Add(detailInfoPanel);

            // 创建五星垫次数卡片
            var borderFiveStar = CreateDetailBorder();
            var stackPanelFiveStar = new StackPanel();
            stackPanelFiveStar.Children.Add(new TextBlock { Text = $"距离上一个S级已经垫了{countSinceLast5Star}发", FontWeight = FontWeights.Bold });

            var selectedCardPool = cardPoolInfo.CardPools.FirstOrDefault(cp => cp.CardPoolId == selectedCardPoolId);
            if (selectedCardPool != null && selectedCardPool.FiveStarPity.HasValue)
            {
                var progressBar5 = CreateProgressBar(countSinceLast5Star, selectedCardPool.FiveStarPity.Value);
                stackPanelFiveStar.Children.Add(progressBar5);
                stackPanelFiveStar.Children.Add(new TextBlock { Text = $"保底{selectedCardPool.FiveStarPity.Value}发", FontSize = 12, Foreground = new SolidColorBrush(Colors.Gray) });
                SharedData.FiveStarPity = selectedCardPool.FiveStarPity.Value;
            }
            borderFiveStar.Child = stackPanelFiveStar;
            contentPanel.Children.Add(borderFiveStar);

            // 创建四星垫次数卡片
            var borderFourStar = CreateDetailBorder();
            var stackPanelFourStar = new StackPanel();
            stackPanelFourStar.Children.Add(new TextBlock { Text = $"距离上一个A级已经抽了{countSinceLast4Star}发", FontWeight = FontWeights.Bold });

            if (selectedCardPool != null && selectedCardPool.FourStarPity.HasValue)
            {
                var progressBar4 = CreateProgressBar(countSinceLast4Star, selectedCardPool.FourStarPity.Value);
                stackPanelFourStar.Children.Add(progressBar4);
                stackPanelFourStar.Children.Add(new TextBlock { Text = $"保底{selectedCardPool.FourStarPity.Value}发", FontSize = 12, Foreground = new SolidColorBrush(Colors.Gray) });
            }
            borderFourStar.Child = stackPanelFourStar;
            contentPanel.Children.Add(borderFourStar);

            scrollView.Content = contentPanel;
            Gacha_Panel.Children.Add(scrollView);
            Logging.Write("Finished displaying gacha details", 0);
        }



        private Border CreateDetailBorder()
        {
            return new Border
            {
                Padding = new Thickness(10),
                Margin = new Thickness(0, 4, 0, 4),
                BorderBrush = new SolidColorBrush(Colors.Gray),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8)
            };
        }

        private ProgressBar CreateProgressBar(int value, int maximum)
        {
            return new ProgressBar
            {
                Minimum = 0,
                Maximum = maximum,
                Value = value,
                Height = 12
            };
        }
    }

    public class RankTypeToBackgroundColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            var qualityLevel = value as string;
            SolidColorBrush brush;

            switch (qualityLevel)
            {
                case "5":
                    // Gold color: #FFE2AC58
                    brush = new SolidColorBrush(ColorHelper.FromArgb(0xFF, 0xE2, 0xAC, 0x58));
                    break;
                case "4":
                    // Purple color: #FF7242B3
                    brush = new SolidColorBrush(ColorHelper.FromArgb(0xFF, 0x72, 0x42, 0xB3));
                    break;
                case "3":
                    // Dark Blue color: #FF3F5992
                    brush = new SolidColorBrush(ColorHelper.FromArgb(0xFF, 0x3F, 0x59, 0x92));
                    break;
                default:
                    brush = new SolidColorBrush(Colors.Transparent);
                    break;
            }
            return brush;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException("Converting from a SolidColorBrush to a string is not supported.");
        }
    }

    public class CountToBackgroundColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            int count = int.Parse(value.ToString());
            SolidColorBrush brush;
            if (count >= 0 && count <= 40)
            {
                brush = new SolidColorBrush(Colors.Green);
            }
            else if (count >= 41 && count <= 70)
            {
                brush = new SolidColorBrush(Colors.Orange);
            }
            else if (count >= 71 && count <= 90)
            {
                brush = new SolidColorBrush(Colors.Red);
            }
            else
            {
                brush = new SolidColorBrush(Colors.Transparent);
            }
            Logging.Write($"Converting count {count} to background color", 0);
            return brush;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }

    public class CountToProgressBackgroundColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            int count = int.Parse(value.ToString());
            SolidColorBrush brush;
            if (count >= 0 && count <= 40)
            {
                brush = new SolidColorBrush(Colors.DarkGreen);
            }
            else if (count >= 41 && count <= 70)
            {
                brush = new SolidColorBrush(Colors.DarkOrange);
            }
            else if (count >= 71 && count <= 90)
            {
                brush = new SolidColorBrush(Colors.DarkRed);
            }
            else
            {
                brush = new SolidColorBrush(Colors.Transparent);
            }
            Logging.Write($"Converting count {count} to progress background color", 0);
            return brush;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }

    public class CountToProgressWidthConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            // 获取 Count 的值
            int count = int.Parse((string)value);

            // 定义最大宽度
            double maxWidth = 290;
            double width = (count / (double)SharedData.FiveStarPity) * maxWidth;
            Logging.Write($"Converting count {count} to progress width {width}", 0);
            return Math.Min(width, maxWidth);
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }

}
