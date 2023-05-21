using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Net.Http;
using System.Diagnostics;

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

        public async Task<List<GachaRecords>> GetAllGachaRecordsAsync(String url)
        {
            var records = new List<GachaRecords>();
            var page = 1;
            var endId = "0";
            int urlindex = url.IndexOf("end_id=");

            while (true)
            {
                var client = new HttpClient();
                await Task.Delay(TimeSpan.FromSeconds(0.1));
                var response = await client.GetAsync(url.Substring(0,urlindex)+"end_id="+endId);
                if (!response.IsSuccessStatusCode) break;
                var json = await response.Content.ReadAsStringAsync();
                var data = JsonSerializer.Deserialize<JsonElement>(json).GetProperty("data");
                Trace.WriteLine(url.Substring(0, urlindex) + "end_id=" + endId);
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
                    Trace.WriteLine(item.GetProperty("uid").GetString()+"|"+item.GetProperty("time").GetString() + "|" + item.GetProperty("gacha_id").GetString() + "|" + item.GetProperty("name").GetString() + "|" + item.GetProperty("id").GetString());
                }
                endId = records.Last().Id;
                page++;
            }
            return records;
        }
    }
}
