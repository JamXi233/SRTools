using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Net.Http;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.IO;
using static SRTools.App;

namespace SRTools.Depend
{
    public class GachaRecords
    {
        
        public async Task<List<GachaRecords>> GetAllGachaRecordsAsync(string url, string localData = null, string gachaType = "11")
        {
            var records = new List<GachaRecords>();
            if (localData == null)
            {
                var client = new HttpClient();
                var page = 1;
                var count = 0;
                var endId = "0";
                int urlIndex = url.IndexOf("&gacha_type=");
                while (true)
                {
                    try
                    {
                        Logging.Write("Wait Timeout...", 0);
                        var newUrl = $"{url.Substring(0, urlIndex)}&gacha_type={gachaType}&end_id={endId}";
                        var response = await client.GetAsync(newUrl);
                        var json = await response.Content.ReadAsStringAsync();
                        var jsonObj = JObject.Parse(json);

                        if (jsonObj["message"].ToString() == "authkey timeout")
                        {
                            records.Add(new GachaRecords { Uid = "authkey timeout" });
                            return records;
                        }
                        if (jsonObj["message"].ToString() == "visit too frequently" && jsonObj["retcode"].ToString() == "-110")
                        {
                            WaitOverlayManager.RaiseWaitOverlay(true, "正在获取API信息,请不要退出", "等待...", true, 0);
                            await Task.Delay(TimeSpan.FromSeconds(0.05));
                            continue;
                        }

                        var data = jsonObj["data"];
                        if (data["list"].Count() == 0) break;

                        foreach (var item in data["list"])
                        {
                            records.Add(new GachaRecords
                            {
                                Uid = item["uid"].ToString(),
                                GachaId = item["gacha_id"].ToString(),
                                GachaType = item["gacha_type"].ToString(),
                                ItemId = item["item_id"].ToString(),
                                Count = item["count"].ToString(),
                                Time = item["time"].ToString(),
                                Name = item["name"].ToString(),
                                Lang = item["lang"].ToString(),
                                ItemType = item["item_type"].ToString(),
                                RankType = item["rank_type"].ToString(),
                                Id = item["id"].ToString()
                            });
                            count++;
                            Logging.Write($"{item["uid"]}|{item["time"]}|{item["gacha_id"]}|{item["name"]}|{item["id"]}", 0);
                            WaitOverlayManager.RaiseWaitOverlay(true, "正在获取API信息,请不要退出", $"已获取{count}条记录{item["uid"]}|{item["time"]}|{item["name"]}", true, 0);
                        }
                        endId = records.Last().Id;
                        page++;
                    }
                    catch (Exception ex)
                    {
                        records.Add(new GachaRecords { Uid = ex.Message });
                        return records;
                    }
                }
            }
            else
            {
                records = JsonConvert.DeserializeObject<List<GachaRecords>>(localData);
            }
            Logging.Write("Gacha Get Finished!", 0);
            return records;
        }

        private static readonly string userDocumentsFolderPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        private static readonly string srtoolsBasePath = Path.Combine(userDocumentsFolderPath, "JSG-LLC", "SRTools");

        public static async Task UpdateGachaRecordsAsync()
        {
            string[] gachaFiles = {
                "GachaRecords_Character.ini",
                "GachaRecords_LightCone.ini",
                "GachaRecords_Newbie.ini",
                "GachaRecords_Regular.ini"
            };

            foreach (var fileName in gachaFiles)
            {
                string filePath = Path.Combine(srtoolsBasePath, fileName);
                if (File.Exists(filePath))
                {
                    try
                    {
                        string fileContent = await File.ReadAllTextAsync(filePath);
                        var newRecords = JsonConvert.DeserializeObject<GachaRecords[]>(fileContent);
                        if (newRecords != null && newRecords.Length > 0)
                        {
                            string uid = newRecords[0].Uid;
                            string targetDirectory = Path.Combine(srtoolsBasePath, "GachaRecords", uid);
                            Directory.CreateDirectory(targetDirectory);
                            string targetFilePath = Path.Combine(targetDirectory, fileName);

                            if (File.Exists(targetFilePath))
                            {
                                string existingContent = await File.ReadAllTextAsync(targetFilePath);
                                var existingRecords = JsonConvert.DeserializeObject<GachaRecords[]>(existingContent);
                                var existingIds = new HashSet<string>(existingRecords.Select(rec => rec.Id));
                                var mergedRecords = existingRecords.ToList();
                                mergedRecords.AddRange(newRecords.Where(rec => !existingIds.Contains(rec.Id)));
                                mergedRecords.Sort((a, b) => b.Time.CompareTo(a.Time));
                                string serializedContent = JsonConvert.SerializeObject(mergedRecords.ToArray());
                                await File.WriteAllTextAsync(targetFilePath, serializedContent);
                            }
                            else
                            {
                                Array.Sort(newRecords, (a, b) => b.Time.CompareTo(a.Time));
                                await File.WriteAllTextAsync(targetFilePath, JsonConvert.SerializeObject(newRecords));
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error processing file {fileName}: {ex.Message}");
                    }
                }
            }
        }
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
}
