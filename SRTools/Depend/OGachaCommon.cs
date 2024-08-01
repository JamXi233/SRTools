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

using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.Storage.Pickers;
using Windows.Storage;
using System.IO;
using SRTools.Views.ToolViews;

namespace SRTools.Depend
{
    class OGachaCommon
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

        public OGachaCommon()
        {
            info = new Info();
            list = new List<Item>();
        }

        public string ToJson()
        {
            return JsonSerializer.Serialize(this);
        }

        public async Task<bool> ExportAll()
        {
            // 定义一个空的列表，用于存储所有的ExportSRGF.OItem对象
            var oitems = new List<OGachaCommon.OItem>();

            // 读取每个文件的内容，并将其反序列化为一个List<ExportSRGF.OItem>对象
            var srtoolsFolder = await KnownFolders.DocumentsLibrary.GetFolderAsync($"JSG-LLC\\SRTools\\GachaRecords\\{GachaView.selectedUid}");
            var files = new List<string> { "GachaRecords_Character.ini", "GachaRecords_LightCone.ini", "GachaRecords_Newbie.ini", "GachaRecords_Regular.ini" };

            foreach (var fileName in files)
            {
                var file1 = await srtoolsFolder.TryGetItemAsync(fileName) as StorageFile;

                if (file1 != null && file1.IsAvailable)
                {
                    var json1 = await FileIO.ReadTextAsync(file1);
                    var list = JsonSerializer.Deserialize<List<OGachaCommon.OItem>>(json1);

                    oitems.AddRange(list);
                }
            }

            // 序列化oitems列表为JSON字符串
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
            OGachaCommon data = new OGachaCommon
            {
                info = new Info
                {
                    uid = oitems.FirstOrDefault()?.Uid,
                    lang = "zh-cn",
                    region_time_zone = 8,
                    export_timestamp = (int)(DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds,
                    export_app = "SRTools",
                    export_app_version = $"{Package.Current.Id.Version.Major}.{Package.Current.Id.Version.Minor}.{Package.Current.Id.Version.Build}.{Package.Current.Id.Version.Revision}",
                    srgf_version = "v1.0"
                },
                list = items
            };

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
            var savePicker = new FileSavePicker
            {
                SuggestedFileName = "SRTools_Gacha_Export_" + data.info.uid + "_" + formattedDate
            };
            savePicker.FileTypeChoices.Add("Star Rail GachaLog Format standard (SRGF) v1.0", new List<string>() { ".json" });

            var window = new Window();
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
            WinRT.Interop.InitializeWithWindow.Initialize(savePicker, hwnd);

            // 显示文件保存对话框
            StorageFile file = await savePicker.PickSaveFileAsync();
            if (file != null)
            {
                // 将字符串写入文件
                await FileIO.WriteTextAsync(file, json);
                return true;
            }
            else
            {
                return false;
            }
        }

        public async Task ImportSRGF()
        {
            string jsonData = await File.ReadAllTextAsync(SharedDatas.UpdateSRGF.UpdateSRGFFilePath);
            var srgfData = JsonSerializer.Deserialize<OGachaCommon>(jsonData);

            // 获取 uid
            var uid = srgfData.info?.uid;
            SharedDatas.UpdateSRGF.UpdateSRGFUID = uid;

            // 创建四个不同的列表
            List<OItem> gachaCharacterList = new List<OItem>();
            List<OItem> gachaLightConeList = new List<OItem>();
            List<OItem> gachaNewbieList = new List<OItem>();
            List<OItem> gachaRegularList = new List<OItem>();

            // 根据导入的数据进行拆分并转换为 OItem
            foreach (var item in srgfData.list)
            {
                OItem oItem = new OItem
                {
                    Uid = uid,
                    GachaId = item.gacha_id,
                    GachaType = item.gacha_type,
                    ItemId = item.item_id,
                    Count = item.count,
                    Time = item.time,
                    Name = item.name,
                    Lang = srgfData.info.lang,
                    ItemType = item.item_type,
                    RankType = item.rank_type,
                    Id = item.id
                };

                // 根据 gacha_type 添加到对应的列表
                switch (item.gacha_type)
                {
                    case "11":
                        gachaCharacterList.Add(oItem);
                        break;
                    case "12":
                        gachaLightConeList.Add(oItem);
                        break;
                    case "2":
                        gachaNewbieList.Add(oItem);
                        break;
                    case "1":
                        gachaRegularList.Add(oItem);
                        break;
                }
            }

            string documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            string srtoolsFolderPath = Path.Combine(documentsPath, "JSG-LLC", "SRTools", "GachaRecords", "tmp", uid);
            Directory.CreateDirectory(srtoolsFolderPath);

            // 导出 JSON 文件
            await ExportJsonFile(srtoolsFolderPath, "GachaRecords_Character.ini", gachaCharacterList);
            await ExportJsonFile(srtoolsFolderPath, "GachaRecords_LightCone.ini", gachaLightConeList);
            await ExportJsonFile(srtoolsFolderPath, "GachaRecords_Newbie.ini", gachaNewbieList);
            await ExportJsonFile(srtoolsFolderPath, "GachaRecords_Regular.ini", gachaRegularList);

            Logging.Write("拆分并导出 JSON 文件完成。");
        }

        private async Task ExportJsonFile(string folderPath, string fileName, List<OItem> list)
        {
            string filePath = Path.Combine(folderPath, fileName);
            string jsonContent = JsonSerializer.Serialize(list);
            await File.WriteAllTextAsync(filePath, jsonContent);

            // 检查并删除空文件
            CheckAndDeleteEmptyFile(folderPath, fileName);
        }

        private void CheckAndDeleteEmptyFile(string folderPath, string fileName)
        {
            string filePath = Path.Combine(folderPath, fileName);
            if (File.Exists(filePath))
            {
                string fileContent = File.ReadAllText(filePath);
                if (fileContent == "[]")
                {
                    File.Delete(filePath);
                    Logging.Write($"已删除空文件: {fileName}");
                }
            }
        }
    }
}
