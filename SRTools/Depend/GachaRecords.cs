using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Net.Http;
using System.Diagnostics;
using System.Xml.Linq;
using Newtonsoft.Json.Linq;
using static Vanara.PInvoke.User32.RAWINPUT;
using Newtonsoft.Json;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml;
using SRTools.Views;

namespace SRTools.Depend
{
    public class GachaRecords
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

        public async Task<List<GachaRecords>> GetAllGachaRecordsAsync(String url, String localData = null , String gachaType = "11")
        {
            var records = new List<GachaRecords>();
            if (localData == null){
                var page = 1;
                var endId = "0";
                int urlindex = url.IndexOf("&gacha_type=");
                while (true)
                {
                    try
                    {
                        var client = new HttpClient();
                        await Task.Delay(TimeSpan.FromSeconds(0.1));
                        Logging.Write("Wait Timeout...", 0);
                        var newurl = url.Substring(0, urlindex) + "&gacha_type=" + gachaType + "&end_id=" + endId;
                        var response = await client.GetAsync(newurl);
                        if (!response.IsSuccessStatusCode) break;
                        var json = await response.Content.ReadAsStringAsync();
                        var data = System.Text.Json.JsonSerializer.Deserialize<JsonElement>(json).GetProperty("data");
                        Logging.Write(url.Substring(0, urlindex) + "&gacha_type=" + gachaType + "&end_id=" + endId, 0);
                        JObject jsonObj = JObject.Parse(json);
                        if (jsonObj["message"].ToString() == "authkey timeout")
                        {
                            records.Add(new GachaRecords
                            {
                                Uid = jsonObj["message"].ToString(),
                                GachaId = "",
                                GachaType = "",
                                ItemId = "",
                                Count = "",
                                Time = "",
                                Name = "",
                                Lang = "",
                                ItemType = "",
                                RankType = "",
                                Id = ""
                            });
                            return records;
                        }
                        else
                        {
                            if (data.GetProperty("list").GetArrayLength() == 0) break;
                            foreach (var item in data.GetProperty("list").EnumerateArray())
                            {
                                records.Add(new GachaRecords
                                {
                                    Uid = item.GetProperty("uid").GetString(),
                                    GachaId = item.GetProperty("gacha_id").GetString(),
                                    GachaType = item.GetProperty("gacha_type").GetString(),
                                    ItemId = item.GetProperty("item_id").GetString(),
                                    Count = item.GetProperty("count").GetString(),
                                    Time = item.GetProperty("time").GetString(),
                                    Name = item.GetProperty("name").GetString(),
                                    Lang = item.GetProperty("lang").GetString(),
                                    ItemType = item.GetProperty("item_type").GetString(),
                                    RankType = item.GetProperty("rank_type").GetString(),
                                    Id = item.GetProperty("id").GetString()
                                });
                                Logging.Write(item.GetProperty("uid").GetString() + "|" + item.GetProperty("time").GetString() + "|" + item.GetProperty("gacha_id").GetString() + "|" + item.GetProperty("name").GetString() + "|" + item.GetProperty("id").GetString(), 0);
                            }
                            endId = records.Last().Id;
                            page++;
                        }
                    }
                    catch (Exception ex)
                    {
                        records.Add(new GachaRecords
                        {
                            Uid = ex.Message,
                            GachaId = "",
                            GachaType = "",
                            ItemId = "",
                            Count = "",
                            Time = "",
                            Name = "",
                            Lang = "",
                            ItemType = "",
                            RankType = "",
                            Id = ""
                        });
                        return records;
                    }
                }
            }
            else {
                records = JsonConvert.DeserializeObject<List<GachaRecords>>(localData);
            }
            Logging.Write("Gacha Get Finished!", 0);
            return records;
        }
    }
}
