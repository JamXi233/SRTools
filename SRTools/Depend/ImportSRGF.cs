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
using SRTools.Depend;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Windows.Storage.Pickers;

public class ImportSRGF
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


    public async Task Main()
    {
        // 导入原始 JSON 数据
        var picker = new FileOpenPicker();
        picker.FileTypeFilter.Add(".json");
        var window = new Window();
        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
        WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);
        var file = await picker.PickSingleFileAsync();
        if (file != null)
        {
            string jsonData = File.ReadAllText(file.Path);

            var srgfData = JsonSerializer.Deserialize<ImportSRGF>(jsonData);

            // 获取 uid
            var uid = srgfData.info?.uid;

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
                if (item.gacha_type == "11")
                    gachaCharacterList.Add(oItem);
                else if (item.gacha_type == "12")
                    gachaLightConeList.Add(oItem);
                else if (item.gacha_type == "2")
                    gachaNewbieList.Add(oItem);
                else if (item.gacha_type == "1")
                    gachaRegularList.Add(oItem);
            }

            string documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            string srtoolsFolderPath = Path.Combine(documentsPath, "JSG-LLC", "SRTools");
            Directory.CreateDirectory(srtoolsFolderPath);

            // 导出 JSON 文件
            await ExportJsonFile(srtoolsFolderPath, "GachaRecords_Character.ini", gachaCharacterList);
            await ExportJsonFile(srtoolsFolderPath, "GachaRecords_LightCone.ini", gachaLightConeList);
            await ExportJsonFile(srtoolsFolderPath, "GachaRecords_Newbie.ini", gachaNewbieList);
            await ExportJsonFile(srtoolsFolderPath, "GachaRecords_Regular.ini", gachaRegularList);


            Logging.Write("拆分并导出 JSON 文件完成。");
        }
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