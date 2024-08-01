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

using Microsoft.UI.Xaml.Controls;
using Newtonsoft.Json;
using SRTools.Views.ToolViews;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using static SRTools.App;

namespace SRTools.Depend
{
    public class GachaCommon
    {
        public class Info
        {
            public string uid { get; set; }
        }

        public class GachaRecord
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

        public class SourceInfo
        {
            public string uid { get; set; }
        }

        public class SourceGachaRecord
        {
            public string gachaId { get; set; }
            public string gachaType { get; set; }
            public string itemId { get; set; }
            public string count { get; set; }
            public string time { get; set; }
            public string name { get; set; }
            public string lang { get; set; }
            public string itemType { get; set; }
            public string rankType { get; set; }
            public string id { get; set; }
        }

        public class SourceRecord
        {
            public int cardPoolId { get; set; }
            public string cardPoolType { get; set; }
            public List<SourceGachaRecord> records { get; set; }
        }

        public class SourceData
        {
            public SourceInfo info { get; set; }
            public List<SourceRecord> list { get; set; }
        }
    }

    public class CardPoolInfo
    {
        public Dictionary<string, string> CardPoolTypes { get; set; }
        public Dictionary<string, int> CardPoolIds { get; set; }
    }

    public class ImportGacha
    {
        public class ImportData
        {
            public GachaCommon.Info info { get; set; }
            public List<GachaCommon.GachaRecord> list { get; set; }
        }

        public class CardPoolRule
        {
            public int cardPoolId { get; set; }
            public string cardPoolType { get; set; }
        }

        public class CardPoolRules
        {
            public List<CardPoolRule> cardPools { get; set; }
        }

        public class ImportUser
        {
            public string uid { get; set; }
            public int timezone { get; set; }
            public string lang { get; set; }
            public List<GachaCommon.GachaRecord> list { get; set; }
        }

        public class ImportFormat
        {
            public ExportGacha.ExportInfo info { get; set; }
            public List<ImportUser> hkrpg { get; set; }
        }

        public static async Task<int> Import(string importFilePath)
        {
            var cardPoolRules = await GetCardPoolRulesAsync();
            var cardPoolInfo = await GetCardPoolInfoAsync();

            var importJson = await File.ReadAllTextAsync(importFilePath);
            var importFormat = JsonConvert.DeserializeObject<ImportFormat>(importJson);

            // 检查文件内的info中是否有srgf_version key
            var jsonData = JsonConvert.DeserializeObject<Dictionary<string, object>>(importJson);
            if (jsonData.ContainsKey("info"))
            {
                var infoData = JsonConvert.DeserializeObject<Dictionary<string, object>>(jsonData["info"].ToString());
                if (infoData.ContainsKey("srgf_version"))
                {
                    NotificationManager.RaiseNotification("导入抽卡记录失败", "文件为旧版本SRGF文件", InfoBarSeverity.Error, true, 3);
                    if (infoData.ContainsKey("uid"))
                    {
                        string updatesrgfuid = infoData["uid"].ToString();
                        SharedDatas.UpdateSRGF.UpdateSRGFUID = updatesrgfuid;
                    }
                    return 2;
                }
            }

            if (importFormat?.hkrpg == null || importFormat.hkrpg.Count == 0)
            {
                NotificationManager.RaiseNotification("导入抽卡记录失败", "未找到hkrpg字段", InfoBarSeverity.Error, true, 3);
                return 1;
            }

            var firstUser = importFormat.hkrpg.First();
            string uid = firstUser.uid;

            if (string.IsNullOrEmpty(uid))
            {
                NotificationManager.RaiseNotification("导入抽卡记录失败", "UID丢失", InfoBarSeverity.Error, true, 3);
                return 1;
            }

            string recordsBasePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), @"JSG-LLC\SRTools\GachaRecords");
            string targetFilePath = Path.Combine(recordsBasePath, $"{uid}.json");

            var sourceData = File.Exists(targetFilePath)
                ? JsonConvert.DeserializeObject<GachaCommon.SourceData>(await File.ReadAllTextAsync(targetFilePath))
                  ?? new GachaCommon.SourceData { info = new GachaCommon.SourceInfo { uid = uid }, list = new List<GachaCommon.SourceRecord>() }
                : new GachaCommon.SourceData { info = new GachaCommon.SourceInfo { uid = uid }, list = new List<GachaCommon.SourceRecord>() };

            var finalSourceData = new GachaCommon.SourceData
            {
                info = new GachaCommon.SourceInfo { uid = uid },
                list = new List<GachaCommon.SourceRecord>()
            };

            // 初始化 finalSourceData 以包含所有 CardPoolType
            foreach (var cardPoolType in cardPoolInfo.CardPoolTypes)
            {
                finalSourceData.list.Add(new GachaCommon.SourceRecord
                {
                    cardPoolId = int.Parse(cardPoolType.Key),
                    cardPoolType = cardPoolType.Value,
                    records = new List<GachaCommon.SourceGachaRecord>()
                });
            }

            // 处理导入记录
            foreach (var importRecord in firstUser.list)
            {
                int cardPoolId = int.Parse(importRecord.gacha_type);
                var cardPoolType = cardPoolRules.FirstOrDefault(r => r.cardPoolId == cardPoolId)?.cardPoolType ?? "未知频段";

                var sourceRecord = new GachaCommon.SourceGachaRecord
                {
                    gachaId = importRecord.gacha_id,
                    gachaType = importRecord.gacha_type,
                    itemId = importRecord.item_id,
                    rankType = importRecord.rank_type,
                    itemType = importRecord.item_type,
                    name = importRecord.name,
                    count = importRecord.count,
                    time = importRecord.time,
                    id = importRecord.id
                };

                var existingSourceRecord = finalSourceData.list.FirstOrDefault(r => r.cardPoolId == cardPoolId);
                if (existingSourceRecord != null)
                {
                    existingSourceRecord.cardPoolType = cardPoolType; // 更新 cardPoolType
                    var existingGachaRecord = existingSourceRecord.records.FirstOrDefault(r => r.id == importRecord.id);
                    if (existingGachaRecord == null)
                    {
                        existingSourceRecord.records.Add(sourceRecord);
                    }
                    else
                    {
                        existingGachaRecord.gachaId = sourceRecord.gachaId;
                        existingGachaRecord.gachaType = sourceRecord.gachaType;
                        existingGachaRecord.itemId = sourceRecord.itemId;
                        existingGachaRecord.rankType = sourceRecord.rankType;
                        existingGachaRecord.itemType = sourceRecord.itemType;
                        existingGachaRecord.name = sourceRecord.name;
                        existingGachaRecord.count = sourceRecord.count;
                        existingGachaRecord.time = sourceRecord.time;
                    }
                }
                else
                {
                    // 如果没有找到相应的 cardPoolId, 创建一个新的记录并添加到 finalSourceData
                    finalSourceData.list.Add(new GachaCommon.SourceRecord
                    {
                        cardPoolId = cardPoolId,
                        cardPoolType = cardPoolType,
                        records = new List<GachaCommon.SourceGachaRecord> { sourceRecord }
                    });
                }
            }

            // 合并已有数据
            foreach (var sourceRecord in sourceData.list)
            {
                var targetRecord = finalSourceData.list.FirstOrDefault(r => r.cardPoolId == sourceRecord.cardPoolId);
                if (targetRecord != null)
                {
                    foreach (var record in sourceRecord.records)
                    {
                        if (!targetRecord.records.Any(r => r.id == record.id))
                        {
                            targetRecord.records.Add(record);
                        }
                    }
                }
                else
                {
                    finalSourceData.list.Add(sourceRecord);
                }
            }

            // 排序和序列化
            finalSourceData.list = SortGachaRecords(finalSourceData.list);
            foreach (var sourceRecord in finalSourceData.list)
            {
                sourceRecord.records = sourceRecord.records.OrderByDescending(record => record.id).ToList();
            }

            var settings = new JsonSerializerSettings
            {
                Formatting = Formatting.Indented,
                NullValueHandling = NullValueHandling.Ignore
            };
            var finalSourceJson = JsonConvert.SerializeObject(finalSourceData, settings);

            Directory.CreateDirectory(recordsBasePath);
            await File.WriteAllTextAsync(targetFilePath, finalSourceJson);
            NotificationManager.RaiseNotification("跃迁记录导入完成", null, InfoBarSeverity.Success, true, 2);
            return 0;
        }


        private static async Task<List<CardPoolRule>> GetCardPoolRulesAsync()
        {
            var cardPoolRules = await GetDataAsync<CardPoolRules>("https://srtools.jamsg.cn/api/cardPoolRule").ConfigureAwait(false);
            return cardPoolRules.cardPools;
        }


        public static async Task<CardPoolInfo> GetCardPoolInfoAsync()
        {
            return await GetDataAsync<CardPoolInfo>("https://srtools.jamsg.cn/api/cardPoolInfo").ConfigureAwait(false);
        }

        private static async Task<T> GetDataAsync<T>(string url)
        {
            using var client = new HttpClient();
            var response = await client.GetStringAsync(url);
            return JsonConvert.DeserializeObject<T>(response);
        }

        private static List<GachaCommon.SourceRecord> SortGachaRecords(List<GachaCommon.SourceRecord> records)
        {
            return records
                .OrderBy(record => GetSortOrder(record.cardPoolId))
                .ThenBy(record => record.cardPoolType == "未知频段" ? 1 : 0)
                .ToList();
        }

        private static int GetSortOrder(int cardPoolId)
        {
            return cardPoolId switch
            {
                11 => 1,
                12 => 2,
                2 => 95,
                1 => 96,
                _ => 100
            };
        }
    }

    public class ExportGacha
    {
        public class ExportInfo
        {
            public long export_timestamp { get; set; }
            public string export_app { get; set; }
            public string export_app_version { get; set; }
            public string version { get; set; } = "v4.0";
        }

        public class ExportUser
        {
            public string uid { get; set; }
            public int timezone { get; set; }
            public string lang { get; set; }
            public List<GachaCommon.GachaRecord> list { get; set; }
        }

        public class ExportData
        {
            public ExportInfo info { get; set; }
            public List<ExportUser> hkrpg { get; set; }
        }

        public static async Task ExportAsync(string sourceFilePath, string exportFilePath)
        {
            var sourceJson = await File.ReadAllTextAsync(sourceFilePath);
            var sourceData = JsonConvert.DeserializeObject<GachaCommon.SourceData>(sourceJson);

            PackageVersion packageVersion = Package.Current.Id.Version;
            string currentVersion = $"{packageVersion.Major}.{packageVersion.Minor}.{packageVersion.Build}.{packageVersion.Revision}";

            var exportData = new ExportData
            {
                info = new ExportInfo
                {
                    export_timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                    export_app = "SRTools",
                    export_app_version = currentVersion,
                    version = "v4.0"
                },
                hkrpg = new List<ExportUser>
            {
                new ExportUser
                {
                    uid = sourceData.info.uid,
                    timezone = 8,
                    lang = "zh-cn",
                    list = new List<GachaCommon.GachaRecord>()
                }
            }
            };

            var timestampCounter = new Dictionary<long, int>();

            // 获取cardPoolInfo
            var cardPoolInfo = await ImportGacha.GetCardPoolInfoAsync();

            foreach (var sourceRecord in sourceData.list.OrderBy(record => GetSortOrder(record.cardPoolId, cardPoolInfo)))
            {
                foreach (var gachaRecord in sourceRecord.records.OrderBy(record => record.time))
                {
                    long timestamp = DateTimeOffset.Parse(gachaRecord.time).ToUnixTimeSeconds();

                    if (!timestampCounter.ContainsKey(timestamp))
                    {
                        timestampCounter[timestamp] = Math.Min(sourceRecord.records.Count(record => DateTimeOffset.Parse(record.time).ToUnixTimeSeconds() == timestamp), 10);
                    }
                    int drawNumber = timestampCounter[timestamp];
                    timestampCounter[timestamp]--;

                    int gachaType = sourceRecord.cardPoolId;

                    exportData.hkrpg[0].list.Add(new GachaCommon.GachaRecord
                    {
                        gacha_id = gachaRecord.gachaId,
                        gacha_type = gachaType.ToString(),
                        item_id = gachaRecord.itemId.ToString(),
                        count = gachaRecord.count.ToString(),
                        time = gachaRecord.time,
                        name = gachaRecord.name,
                        item_type = gachaRecord.itemType,
                        rank_type = gachaRecord.rankType.ToString(),
                        id = gachaRecord.id
                    });
                }
            }

            var exportJson = JsonConvert.SerializeObject(exportData, Formatting.Indented);
            await File.WriteAllTextAsync(exportFilePath, exportJson);
            NotificationManager.RaiseNotification("抽卡记录导出完成", $"文件已保存到\n{exportFilePath}", InfoBarSeverity.Success, true, 2);
        }

        private static int GetSortOrder(int cardPoolId, CardPoolInfo cardPoolInfo)
        {
            return cardPoolId switch
            {
                var id when id == cardPoolInfo.CardPoolIds["11"] => 1,
                var id when id == cardPoolInfo.CardPoolIds["12"] => 2,
                var id when id == cardPoolInfo.CardPoolIds["2"] => 3,
                var id when id == cardPoolInfo.CardPoolIds["1"] => 4,
                _ => 5
            };
        }
    }
}
