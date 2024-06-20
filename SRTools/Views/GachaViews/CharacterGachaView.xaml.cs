using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using Newtonsoft.Json;
using SRTools.Depend;
using Microsoft.UI.Text;
using Microsoft.UI;
using SRTools.Views.ToolViews;
using static SRTools.Depend.GachaModel;
using Windows.Storage;

namespace SRTools.Views.GachaViews
{
    public sealed partial class CharacterGachaView : Page
    {
        public static GachaModel.CardPoolInfo selectedCardPool;
        public CharacterGachaView()
        {
            this.InitializeComponent();
            Logging.Write("Switch to CharacterGachaView", 0);
            selectedCardPool = new GachaModel.CardPoolInfo { FiveStarPity = 90, FourStarPity = 10 }; // 替换成实际获取卡池信息的逻辑
            LoadData();
        }

        private async void LoadData()
        {
            Logging.Write("Starting LoadData method", 0);
            string selectedUID = GachaView.GetSelectedUid();
            Logging.Write($"Selected UID: {selectedUID}", 0);

            var folder = KnownFolders.DocumentsLibrary;
            var srtoolsFolder = await folder.GetFolderAsync($"JSG-LLC\\SRTools\\GachaRecords\\{selectedUID}\\");
            var settingsFile = await srtoolsFolder.GetFileAsync("GachaRecords_Character.ini");
            string gachaRecordsContent = await FileIO.ReadTextAsync(settingsFile);

            var records = JsonConvert.DeserializeObject<List<GachaRecord>>(gachaRecordsContent);
            Logging.Write($"Total records found: {records.Count}", 0);

            var rank4Records = records.Where(r => r.RankType == "4").ToList();
            var rank5Records = records.Where(r => r.RankType == "5").ToList();
            Logging.Write($"4-star records count: {rank4Records.Count}, 5-star records count: {rank5Records.Count}", 0);

            DisplayGachaDetails(records, rank4Records, rank5Records);
            DisplayGachaInfo(records);
            DisplayGachaRecords(records);

            Logging.Write("LoadData method finished", 0);
        }

        private void DisplayGachaRecords(List<GachaRecord> records)
        {
            Logging.Write("Displaying gacha records", 0);
            GachaRecords_List.ItemsSource = records;
        }

        private void DisplayGachaInfo(List<GachaRecord> records)
        {
            Logging.Write("Displaying gacha info", 0);

            var rank5Records = records.Where(r => r.RankType == "5")
                                       .Select(r => new
                                       {
                                           r.Name,
                                           Count = CalculateCount(records, r.Id, "5"),
                                           Pity = CalculatePity(records, r.Name, "5"),
                                           PityVisibility = Visibility.Visible
                                       }).ToList();
            if (rank5Records.Count == 0) GachaInfo_List_Disable.Visibility = Visibility.Visible;

            GachaInfo_List.ItemsSource = rank5Records;
            Logging.Write("Finished displaying gacha info", 0);
        }

        private string CalculateCount(List<GachaRecord> records, string id, string qualityLevel)
        {
            Logging.Write("Calculating count since last target star", 0);
            int countSinceLastTargetStar = 1;
            bool foundTargetStar = false;
            for (int i = records.Count - 1; i >= 0; i--)
            {
                var record = records[i];
                if (record.RankType == qualityLevel && record.Id == id)
                {
                    foundTargetStar = true;
                    break;
                }
                if (record.RankType == "5")
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

        private string CalculatePity(List<GachaRecord> records, string name, string qualityLevel)
        {
            Logging.Write("Calculating pity", 0);
            var specialNames = new List<string> { "布洛妮娅", "杰帕德", "白露", "瓦尔特", "彦卿", "姬子", "克拉拉" };

            if (specialNames.Contains(name))
            {
                Logging.Write("Pity result: 歪了", 0);
                return "歪了";
            }
            else
            {
                Logging.Write("Pity result: 没歪", 0);
                return "";
            }
        }

        private void DisplayGachaDetails(List<GachaRecord> records, List<GachaRecord> rank4Records, List<GachaRecord> rank5Records)
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

            var selectedRecords = records
                .OrderByDescending(r => r.Time)
                .ToList();

            Logging.Write($"Total selected records: {selectedRecords.Count}", 0);

            int countSinceLast5Star = 0;
            int countSinceLast4Star = 0;
            bool foundLast5Star = false;
            bool foundLast4Star = false;

            foreach (var record in selectedRecords)
            {
                if (!foundLast5Star && record.RankType == "5")
                {
                    foundLast5Star = true;
                    foundLast4Star = true;
                }
                else if (!foundLast5Star)
                {
                    countSinceLast5Star++;
                }

                if (!foundLast4Star && record.RankType == "4")
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

            var fourStarIntervals = CalculateIntervals(selectedRecords, "4");
            var fiveStarIntervals = CalculateIntervals(selectedRecords, "5");

            string averageDraws4Star = fourStarIntervals.Count > 0 ? (fourStarIntervals.Average()).ToString("F2") : "∞";
            string averageDraws5Star = fiveStarIntervals.Count > 0 ? (fiveStarIntervals.Average()).ToString("F2") : "∞";

            Gacha_UID.Text = selectedRecords.First().Uid;
            GachaRecords_Count.Text = "共" + selectedRecords.Count() + "抽";
            GachaInfo_SinceLast5Star.Text = $"垫了{(countSinceLast5Star)}发";

            var basicInfoPanel = CreateDetailBorder();
            var stackPanelBasicInfo = new StackPanel();
            stackPanelBasicInfo.Children.Add(new TextBlock { Text = $"UID: {selectedRecords.First().Uid}", FontWeight = FontWeights.Bold });
            stackPanelBasicInfo.Children.Add(new TextBlock { Text = $"总计抽数: {selectedRecords.Count}" });
            stackPanelBasicInfo.Children.Add(new TextBlock { Text = $"五星抽卡次数: {rank5Records.Count}" });
            stackPanelBasicInfo.Children.Add(new TextBlock { Text = $"四星抽卡次数: {rank4Records.Count}" });
            stackPanelBasicInfo.Children.Add(new TextBlock { Text = $"预计使用星琼: {selectedRecords.Count * 160}" });
            basicInfoPanel.Child = stackPanelBasicInfo;
            contentPanel.Children.Add(basicInfoPanel);

            var detailInfoPanel = CreateDetailBorder();
            var stackPanelDetailInfo = new StackPanel();
            stackPanelDetailInfo.Children.Add(new TextBlock { Text = "详细统计", FontWeight = FontWeights.Bold });

            stackPanelDetailInfo.Children.Add(new TextBlock { Text = $"五星平均抽数: {averageDraws5Star}抽" });
            stackPanelDetailInfo.Children.Add(new TextBlock { Text = $"四星平均抽数: {averageDraws4Star}抽" });

            string rate4Star = rank4Records.Count > 0 ? (rank4Records.Count / (double)selectedRecords.Count * 100).ToString("F2") + "%" : "∞";
            string rate5Star = rank5Records.Count > 0 ? (rank5Records.Count / (double)selectedRecords.Count * 100).ToString("F2") + "%" : "∞";

            stackPanelDetailInfo.Children.Add(new TextBlock { Text = $"五星获取率: {rate5Star}" });
            stackPanelDetailInfo.Children.Add(new TextBlock { Text = $"四星获取率: {rate4Star}" });

            if (rank5Records.Any())
            {
                stackPanelDetailInfo.Children.Add(new TextBlock { Text = $"最近五星: {rank5Records.First().Time}" });
            }
            else
            {
                stackPanelDetailInfo.Children.Add(new TextBlock { Text = "最近五星: ∞" });
            }

            if (rank4Records.Any())
            {
                stackPanelDetailInfo.Children.Add(new TextBlock { Text = $"最近四星: {rank4Records.First().Time}" });
            }
            else
            {
                stackPanelDetailInfo.Children.Add(new TextBlock { Text = "最近四星: ∞" });
            }

            detailInfoPanel.Child = stackPanelDetailInfo;
            contentPanel.Children.Add(detailInfoPanel);

            var borderFiveStar = CreateDetailBorder();
            var stackPanelFiveStar = new StackPanel();
            stackPanelFiveStar.Children.Add(new TextBlock { Text = $"距离上一个五星已经垫了{countSinceLast5Star}发", FontWeight = FontWeights.Bold });
            if (selectedCardPool != null && selectedCardPool.FiveStarPity.HasValue)
            {
                var progressBar5 = CreateProgressBar(countSinceLast5Star, selectedCardPool.FiveStarPity.Value);
                stackPanelFiveStar.Children.Add(progressBar5);
                stackPanelFiveStar.Children.Add(new TextBlock { Text = $"保底{selectedCardPool.FiveStarPity.Value}发", FontSize = 12, Foreground = new SolidColorBrush(Colors.Gray) });
            }
            borderFiveStar.Child = stackPanelFiveStar;
            contentPanel.Children.Add(borderFiveStar);

            var borderFourStar = CreateDetailBorder();
            var stackPanelFourStar = new StackPanel();
            stackPanelFourStar.Children.Add(new TextBlock { Text = $"距离上一个四星已经抽了{countSinceLast4Star}发", FontWeight = FontWeights.Bold });

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

        private List<int> CalculateIntervals(List<GachaRecord> records, string qualityLevel)
        {
            var intervals = new List<int>();
            int countSinceLastStar = 0;

            foreach (var record in records.AsEnumerable().Reverse())
            {
                countSinceLastStar++;

                if (record.RankType == qualityLevel)
                {
                    intervals.Add(countSinceLastStar);
                    countSinceLastStar = 0;
                }
            }

            return intervals;
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
                    brush = new SolidColorBrush(ColorHelper.FromArgb(0xFF, 0xE2, 0xAC, 0x58));
                    break;
                case "4":
                    brush = new SolidColorBrush(ColorHelper.FromArgb(0xFF, 0x72, 0x42, 0xB3));
                    break;
                case "3":
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
            if (count >= 0 && count <= 50)
            {
                brush = new SolidColorBrush(Colors.Green);
            }
            else if (count > 50 && count <= 74)
            {
                brush = new SolidColorBrush(Colors.Orange);
            }
            else if (count > 75)
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
            if (count >= 0 && count <= 50)
            {
                brush = new SolidColorBrush(Colors.DarkGreen);
            }
            else if (count > 50 && count <= 74)
            {
                brush = new SolidColorBrush(Colors.DarkOrange);
            }
            else if (count > 75)
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
            int count = int.Parse((string)value);
            double maxWidth = 290;
            double width = count / (double)CharacterGachaView.selectedCardPool.FiveStarPity * maxWidth;
            Logging.Write($"Converting count {count} to progress width {width}", 0);
            return Math.Min(width, maxWidth);
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }

}
