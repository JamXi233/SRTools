using Newtonsoft.Json.Linq;
using System.Linq;
using System.IO;
using Windows.Storage;
using System;
using Newtonsoft.Json;
using Spectre.Console;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SRTools.Depend
{
    public class GetNotify
    {
        public string post_id { get; set; }
        public string type { get; set; }
        public string tittle { get; set; }
        public string url { get; set; }
        public string show_time { get; set; }
        public string order { get; set; }
        public string title { get; set; }
        public async Task Get() 
        {
            string apiAddress = "https://api-launcher-static.mihoyo.com/hkrpg_cn/mdk/launcher/api/content?filter_adv=false&key=6KcVuOkbcqjJomjZ&language=zh-cn&launcher_id=33";

            // 将API响应转换为JSON对象并筛选特定类型的帖子
            var jsonResponse = JObject.Parse(new System.Net.WebClient().DownloadString(apiAddress));
            var activityPosts = jsonResponse["data"]["post"].Where(p => (string)p["type"] == "POST_TYPE_ACTIVITY").OrderByDescending(p => (string)p["type"]).ToList();
            var announcePosts = jsonResponse["data"]["post"].Where(p => (string)p["type"] == "POST_TYPE_ANNOUNCE").OrderByDescending(p => (string)p["type"]).ToList();
            var infoPosts = jsonResponse["data"]["post"].Where(p => (string)p["type"] == "POST_TYPE_INFO").OrderByDescending(p => (string)p["type"]).ToList();

            //将结果保存到文件中
            var folder = KnownFolders.DocumentsLibrary;
            var srtoolsFolder = await folder.CreateFolderAsync("JSG-LLC\\SRTools", CreationCollisionOption.OpenIfExists);
            var activityFile = await srtoolsFolder.CreateFileAsync("Posts\\activity.json", CreationCollisionOption.OpenIfExists);
            var announceFile = await srtoolsFolder.CreateFileAsync("Posts\\announce.json", CreationCollisionOption.OpenIfExists);
            var infoFile = await srtoolsFolder.CreateFileAsync("Posts\\info.json", CreationCollisionOption.OpenIfExists);
            File.WriteAllText(activityFile.Path, JArray.FromObject(activityPosts).ToString());
            File.WriteAllText(announceFile.Path, JArray.FromObject(announcePosts).ToString());
            File.WriteAllText(infoFile.Path, JArray.FromObject(infoPosts).ToString());

        }
        public List<GetNotify> GetData(String localData)
        {
            var records = new List<GetNotify>();
            records = JsonConvert.DeserializeObject<List<GetNotify>>(localData);
            return records;
        }
    }
}
