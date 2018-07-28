using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using CefSharp;
using CefSharp.Wpf;

namespace Anime47VideoDownloader
{
    delegate void ExecuteHander(object sender, List<VideoUrlsInformation> result);
    class QueueExecuting
    {
        public List<string> URLCollection { get; set; }
        public ChromiumWebBrowser Executer { get; set; }
        public Dispatcher ControlOwner { get; set; }
        public Thread ExecutingThread { get; set; }
        public List<VideoUrlsInformation> VideoUrls { get; }
        public event ExecuteHander Execute_Complete;
        public int ExecuterID { get; set; }

        public QueueExecuting(ChromiumWebBrowser excuter, Dispatcher controlOwner, int id)
        {
            ExecuterID = id;
            VideoUrls = new List<VideoUrlsInformation>();
            URLCollection = new List<string>();
            Executer = excuter;
            ControlOwner = controlOwner;
            ExecutingThread = new Thread(ExecuteAction) { IsBackground = true, Name = "Executer" + ExecuterID.ToString() };
        }

        public void AddUrl(string url)
        {
            URLCollection.Add(url);
        }

        public void Execute()
        {
            ExecutingThread.Start();
        }

        private void ExecuteAction()
        {
            //Since CefSharp Evaluate "document.getElementByClasName('a_class_name')" return null so we need to use Regular Expression
            foreach (var item in URLCollection)
            {
                Executer.Load(item);
                Thread.Sleep(10000);
                string source = Executer.GetSourceAsync().Result;
                Regex regex = new Regex(@"<script type=""text/javascript"">var(.*?)</script>", RegexOptions.Singleline);
                string videoElementContainer = regex.Match(source).Value;
                //Google APIs
                if(videoElementContainer.IndexOf("googleapis") != -1)
                {
                    VideoUrls.Add(new VideoUrlsInformation(item, "Found Google APIs (this is old server 1 which is down in mid 2017, rip a really good server to watch anime :( ) all video (all mean video in Anime47.com) in Google APIs already be removed in mid 2017 so we can't get video from this server, The process will stop, Would you like to download from Openload.co? "));
                    OnExecute_Complete();
                    return;
                }

                //default server which is //video.xx.fbcdn.net
                Regex getVideoURL = new Regex(@"//(.*?)'", RegexOptions.Singleline);
                string VideoURL = getVideoURL.Match(videoElementContainer).Value.Replace("'", "");
                
                //Zing Tv
                if (videoElementContainer.IndexOf(".zadn.vn") != -1)
                {
                    getVideoURL = new Regex(@"//(.*?)""", RegexOptions.Singleline);
                    VideoURL = getVideoURL.Match(videoElementContainer).Value.Replace("\"", "");
                }

                VideoUrls.Add(new VideoUrlsInformation(item, VideoURL));
            }
            OnExecute_Complete();
            
        }

        private void OnExecute_Complete()
        {
            Execute_Complete?.Invoke(this, VideoUrls);
        }
    }
   
}
