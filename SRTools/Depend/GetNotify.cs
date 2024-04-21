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

using Newtonsoft.Json.Linq;
using System.Linq;
using System.IO;
using Windows.Storage;
using System;
using Newtonsoft.Json;
using Spectre.Console;
using System.Collections.Generic;
using System.Net.Http;
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

            using (HttpClient client = new HttpClient())
            {
                // 使用 HttpClient 发送请求并获取响应
                string jsonResponse = await client.GetStringAsync(apiAddress);

                // 将API响应转换为JSON对象并筛选特定类型的帖子
                var jsonObject = JObject.Parse(jsonResponse);
                var activityPosts = jsonObject["data"]["post"]
                    .Where(p => (string)p["type"] == "POST_TYPE_ACTIVITY")
                    .OrderByDescending(p => (string)p["type"])
                    .ToList();
                var announcePosts = jsonObject["data"]["post"]
                    .Where(p => (string)p["type"] == "POST_TYPE_ANNOUNCE")
                    .OrderByDescending(p => (string)p["type"])
                    .ToList();
                var infoPosts = jsonObject["data"]["post"]
                    .Where(p => (string)p["type"] == "POST_TYPE_INFO")
                    .OrderByDescending(p => (string)p["type"])
                    .ToList();

                // 将结果保存到文件中
                var folder = KnownFolders.DocumentsLibrary;
                var srtoolsFolder = await folder.CreateFolderAsync("JSG-LLC\\SRTools", CreationCollisionOption.OpenIfExists);
                var activityFile = await srtoolsFolder.CreateFileAsync("Posts\\activity.json", CreationCollisionOption.OpenIfExists);
                var announceFile = await srtoolsFolder.CreateFileAsync("Posts\\announce.json", CreationCollisionOption.OpenIfExists);
                var infoFile = await srtoolsFolder.CreateFileAsync("Posts\\info.json", CreationCollisionOption.OpenIfExists);
                await File.WriteAllTextAsync(activityFile.Path, JArray.FromObject(activityPosts).ToString());
                await File.WriteAllTextAsync(announceFile.Path, JArray.FromObject(announcePosts).ToString());
                await File.WriteAllTextAsync(infoFile.Path, JArray.FromObject(infoPosts).ToString());
            }
        }

        public List<GetNotify> GetData(string localData)
        {
            var records = JsonConvert.DeserializeObject<List<GetNotify>>(localData);
            return records;
        }
    }
}
