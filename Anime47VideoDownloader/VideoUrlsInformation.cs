using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Anime47VideoDownloader
{
    class VideoUrlsInformation
    {
        public string WebEmbededUrl { get; set; }
        public string VideoUrl { get; set; }
        public string FilenameAfterDownload { get; set; }
        public string RedirectedVideoLink { get; set; }

        public VideoUrlsInformation(string webEmbedUrls, string videoUrls)
        {
            WebEmbededUrl = webEmbedUrls;
            VideoUrl = "http:" + videoUrls;
            SetFilename();
        }

        private void SetFilename()
        {
            string[] rawName = WebEmbededUrl.Split('/')[3].Substring(9).Split('-');
            string result = string.Empty;
            foreach (var item in rawName)
            {
                string temp = item[0].ToString().ToUpper() + item.Substring(1);
                result += temp + " ";
            }
            FilenameAfterDownload = result.Substring(0, result.Length - 1);

        }
    }
}
