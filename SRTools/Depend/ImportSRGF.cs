﻿using Microsoft.UI.Xaml;
using SRTools.Depend;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Pickers;
using static Vanara.PInvoke.Kernel32.FILE_REMOTE_PROTOCOL_INFO;

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

            var srgfData = System.Text.Json.JsonSerializer.Deserialize<ImportSRGF>(jsonData);

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

            var folder = KnownFolders.DocumentsLibrary;
            var srtoolsFolder = await folder.CreateFolderAsync("JSG-LLC\\SRTools", CreationCollisionOption.OpenIfExists);

            // 导出 GachaRecords_Character JSON
            string gachaCharacterJson = JsonSerializer.Serialize(gachaCharacterList);
            File.WriteAllText(srtoolsFolder.Path + "\\GachaRecords_Character.ini", gachaCharacterJson);
            await CheckAndDeleteEmptyFile(srtoolsFolder, "GachaRecords_Character.ini");
            // 导出 GachaRecords_LightCone JSON
            string gachaLightConeJson = JsonSerializer.Serialize(gachaLightConeList);
            File.WriteAllText(srtoolsFolder.Path + "\\GachaRecords_LightCone.ini", gachaLightConeJson);
            await CheckAndDeleteEmptyFile(srtoolsFolder, "GachaRecords_LightCone.ini");
            // 导出 GachaRecords_Newbie JSON
            string gachaNewbieJson = JsonSerializer.Serialize(gachaNewbieList);
            File.WriteAllText(srtoolsFolder.Path + "\\GachaRecords_Newbie.ini", gachaNewbieJson);
            await CheckAndDeleteEmptyFile(srtoolsFolder, "GachaRecords_Newbie.ini");
            // 导出 GachaRecords_Regular JSON
            string gachaRegularJson = JsonSerializer.Serialize(gachaRegularList);
            File.WriteAllText(srtoolsFolder.Path + "\\GachaRecords_Regular.ini", gachaRegularJson);
            await CheckAndDeleteEmptyFile(srtoolsFolder, "GachaRecords_Regular.ini");

            Logging.Write("拆分并导出 JSON 文件完成。");
        }
    }

    private async Task CheckAndDeleteEmptyFile(StorageFolder folder, string fileName)
    {
        var file = await folder.GetFileAsync(fileName);
        var fileContent = await FileIO.ReadTextAsync(file);
        if (fileContent == "[]")
        {
            await file.DeleteAsync();
            Logging.Write($"已删除空文件: {fileName}");
        }
    }

}