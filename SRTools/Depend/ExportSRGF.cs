using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Threading.Tasks;
using Windows.Storage.Pickers;
using Windows.Storage;
using Windows.ApplicationModel;

namespace SRTools.Depend
{
    public class ExportSRGF
    {
        public class Info
        {
            public string uid { get; set; }
            public string lang { get; set; }
            public int region_time_zone { get; set; }
            public int export_timestamp { get; set; }
            public string export_app { get; set; }
            public string export_app_version { get; set; }
            public string srgf_version { get; set; }
        }

        public class OItem
        {
            public string Uid { get; set; }
            public string GachaId { get; set; }
            public string GachaType { get; set; }
            public string ItemId { get; set; }
            public string Count { get; set; }
            public string Time { get; set; }
            public string Name { get; set; }
            public string Lang { get; set; }
            public string ItemType { get; set; }
            public string RankType { get; set; }
            public string Id { get; set; }
        }

        public class Item
        {
            public string gacha_id { get; set; }
            public string gacha_type { get; set; }
            public string item_id { get; set; }
            public string count { get; set; }
            public string time { get; set; }
            public string name { get; set; }
            public string item_type { get; set; }
            public string rank_type { get; set; }
            public string id { get; set; }
        }

        public Info info { get; set; }
        public List<Item> list { get; set; }

        public ExportSRGF()
        {
            info = new Info();
            list = new List<Item>();
        }

        public string ToJson()
        {
            return JsonSerializer.Serialize(this);
        }

        public async void ExportAll() 
        {
            // 定义一个空的列表，用于存储所有的ExportSRGF.OItem对象
            var oitems = new List<ExportSRGF.OItem>();

            // 读取每个文件的内容，并将其反序列化为一个List<ExportSRGF.OItem>对象
            var srtoolsFolder = await KnownFolders.DocumentsLibrary.GetFolderAsync("JSG-LLC\\SRTools");
            var files = new List<string> { "GachaRecords_Character.ini", "GachaRecords_LightCone.ini", "GachaRecords_Newbie.ini", "GachaRecords_Regular.ini" };
            foreach (var fileName in files)
            {
                var file1 = await srtoolsFolder.GetFileAsync(fileName);
                var json1 = await FileIO.ReadTextAsync(file1);
                var list = JsonSerializer.Deserialize<List<ExportSRGF.OItem>>(json1);

                // 将每个List<ExportSRGF.OItem>对象中的所有元素添加到oitems列表中
                oitems.AddRange(list);
            }

            // 序列化oitems列表为JSON字符串
            string jsonOutput = JsonSerializer.Serialize(oitems);
            List<Item> items = oitems.Select(oItem => new Item
            {
                gacha_id = oItem.GachaId,
                gacha_type = oItem.GachaType,
                item_id = oItem.ItemId,
                count = oItem.Count,
                time = oItem.Time,
                name = oItem.Name,
                item_type = oItem.ItemType,
                rank_type = oItem.RankType,
                id = oItem.Id
            }).ToList();
            ExportSRGF data = new ExportSRGF();
            PackageVersion packageVersion = Package.Current.Id.Version;
            string version = $"{packageVersion.Major}.{packageVersion.Minor}.{packageVersion.Build}.{packageVersion.Revision}";
            var uid = oitems.FirstOrDefault()?.Uid;
            data.info.uid = uid;
            data.info.lang = "zh-cn";
            data.info.region_time_zone = 8;
            data.info.export_timestamp = (int)(DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds;
            data.info.export_app = "SRTools";
            data.info.export_app_version = version;
            data.info.srgf_version = "v1.0";
            data.list = items;

            // 配置JsonSerializerOptions对象，设置Encoder属性为不转义中文字符的JavaScriptEncoder
            var options = new JsonSerializerOptions
            {
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                WriteIndented = true // 设置缩进以便更易于阅读
            };

            // 将数据模板转换为JSON字符串
            string json = JsonSerializer.Serialize(data, options);

            // 创建文件保存对话框
            DateTime now = DateTime.Now;
            string formattedDate = now.ToString("yyyy_MM_dd_HH_mm_ss");
            var savePicker = new FileSavePicker();
            savePicker.FileTypeChoices.Add("Star Rail GachaLog Format standard (SRGF) v1.0", new List<string>() { ".json" });
            savePicker.SuggestedFileName = "SRTools_Gacha_Export_"+data.info.uid+"_" + formattedDate;
            var window = new Window();
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
            WinRT.Interop.InitializeWithWindow.Initialize(savePicker, hwnd);

            // 显示文件保存对话框
            StorageFile file = await savePicker.PickSaveFileAsync();
            if (file != null)
            {
                // 将字符串写入文件
                await FileIO.WriteTextAsync(file, json);
            }
        }
    }
}
