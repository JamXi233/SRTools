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
        public string id { get; set; }
        public string type { get; set; }
        public string title { get; set; }
        public string link { get; set; }
        public string date { get; set; }

        public async Task Get()
        {
            string apiAddress = "https://hyp-api.mihoyo.com/hyp/hyp-connect/api/getGameContent?launcher_id=jGHBHlcOq1&game_id=64kMb5iAWu&language=zh-cn";

            using (HttpClient client = new HttpClient())
            {
                // 使用 HttpClient 发送请求并获取响应
                string jsonResponse = await client.GetStringAsync(apiAddress);

                // 将API响应转换为JSON对象并筛选特定类型的帖子
                var jsonObject = JObject.Parse(jsonResponse);
                var activityPosts = jsonObject["data"]["content"]["posts"]
                    .Where(p => (string)p["type"] == "POST_TYPE_ACTIVITY")
                    .OrderByDescending(p => (string)p["type"])
                    .ToList();
                var announcePosts = jsonObject["data"]["content"]["posts"]
                    .Where(p => (string)p["type"] == "POST_TYPE_ANNOUNCE")
                    .OrderByDescending(p => (string)p["type"])
                    .ToList();
                var infoPosts = jsonObject["data"]["content"]["posts"]
                    .Where(p => (string)p["type"] == "POST_TYPE_INFO")
                    .OrderByDescending(p => (string)p["type"])
                    .ToList();

                // 获取用户文档目录下的JSG-LLC\SRTools\Posts目录
                string documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                string srtoolsFolderPath = Path.Combine(documentsPath, "JSG-LLC", "SRTools", "Posts");

                // 确保目录存在
                Directory.CreateDirectory(srtoolsFolderPath);

                // 文件路径
                string activityFilePath = Path.Combine(srtoolsFolderPath, "activity.json");
                string announceFilePath = Path.Combine(srtoolsFolderPath, "announce.json");
                string infoFilePath = Path.Combine(srtoolsFolderPath, "info.json");

                // 将结果保存到文件中
                File.WriteAllTextAsync(activityFilePath, JArray.FromObject(activityPosts).ToString());
                File.WriteAllTextAsync(announceFilePath, JArray.FromObject(announcePosts).ToString());
                File.WriteAllTextAsync(infoFilePath, JArray.FromObject(infoPosts).ToString());
            }
        }

        public List<GetNotify> GetData(string localData)
        {
            var records = JsonConvert.DeserializeObject<List<GetNotify>>(localData);
            return records;
        }
    }
}
