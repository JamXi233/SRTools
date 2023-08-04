using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;

namespace SRTools.Depend
{
    public class ExceptionSave
    {
        public static async Task Write(string message, int severity, string fileName)
        {
            // 获取用户文档目录下的JSG-LLC\Panic目录
            StorageFolder folder = await KnownFolders.DocumentsLibrary.CreateFolderAsync("JSG-LLC\\Panic", CreationCollisionOption.OpenIfExists);

            // 创建或覆盖log.txt文件
            StorageFile file = await folder.CreateFileAsync(fileName, CreationCollisionOption.ReplaceExisting);

            // 将ex变量内容写入文件
            using (StreamWriter writer = new StreamWriter(await file.OpenStreamForWriteAsync()))
            {
                await writer.WriteLineAsync(DateTime.Now.ToString() + " [" + severity.ToString() + "] \n" + message);
            }
        }
    }
}
