using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Text.RegularExpressions;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Threading;
using CefSharp;
using System.Net;
using CefSharp.Wpf;
using System.IO;
using System.ComponentModel;
using System.Media;
using Microsoft.Win32;

namespace Anime47VideoDownloader
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    /// 
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        #region Fields and Properties
        Monitor monitor = new Monitor();
        Thread getEpAsync;
        List<string> allEp = new List<string>();
        List<VideoUrlsInformation> videoInformationUrl = new List<VideoUrlsInformation>();
        List<QueueExecuting> queues = new List<QueueExecuting>();
        const string explain = "Found Google APIs (this is old server 1 which is down in mid 2017, rip a really good server to watch anime :( ) all video (all mean video in Anime47.com) in Google APIs already be removed in mid 2017 so we can't get video from this server, The process will stop, Would you like to download from Openload.co? ";
        int workerDone = 0;
        ManualResetEvent threadBlocker = new ManualResetEvent(false);
        ManualResetEvent threadBlocker2 = new ManualResetEvent(false);
        SoundPlayer player = new SoundPlayer(Properties.Resources.GamerOP);
        public event PropertyChangedEventHandler PropertyChanged;
        
        #endregion

        public MainWindow()
        {
            InitializeComponent();
            WriteOutput("Ready!");
            Closed += (s, e) => 
            {
                if(txbStatus.Text != "Idle")
                {
                    if (MessageBox.Show("Download not complete, Do you want to quit anyway?", "Uncompleted Download", MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No) == MessageBoxResult.No)
                        return;
                }
                Environment.Exit(0);
            };
            getEpAsync = new Thread(GetEP) { IsBackground = true, Name="MainExecutionThread" };
            getEpAsync.SetApartmentState(ApartmentState.STA);
            monitor.Loaded += (s, e) => { btnDebug.IsEnabled = true;};

            player.PlayLooping();
        }

        private void AddQueues()
        {
            queues.Add(new QueueExecuting(monitor.b1, Dispatcher, 0));
            queues.Add(new QueueExecuting(monitor.b2, Dispatcher, 1));
            queues.Add(new QueueExecuting(monitor.b3, Dispatcher, 2));
            queues.Add(new QueueExecuting(monitor.b4, Dispatcher, 3));
            queues.Add(new QueueExecuting(monitor.b5, Dispatcher, 4));
            queues.Add(new QueueExecuting(monitor.b6, Dispatcher, 5));
            queues.Add(new QueueExecuting(monitor.b7, Dispatcher, 6));
            queues.Add(new QueueExecuting(monitor.b8, Dispatcher, 7));
            queues.Add(new QueueExecuting(monitor.b9, Dispatcher, 8));
            queues.Add(new QueueExecuting(monitor.b10, Dispatcher, 9));
            queues.Add(new QueueExecuting(monitor.b11, Dispatcher, 10));
            queues.Add(new QueueExecuting(monitor.b12, Dispatcher, 11));
        }

        
        

        private void GetEP()
        {
            WriteOutput("Initializing...");
            while(true)
            {
                bool isInit = false;
                Dispatcher.Invoke(() => { isInit = monitor.b1.IsBrowserInitialized; });
                if (isInit)
                    break;
                else
                    Thread.Sleep(3000);
            }
            
            WriteOutput("Getting All episode!");
            Dispatcher.Invoke(() => { monitor.b1.Load(txbURL.Text); });
            Thread.Sleep(15000);
            string javascript = String.Format("document.getElementsByClassName('btn btn-red')[0].click()");
            monitor.b1.EvaluateScriptAsync(javascript);
            Thread.Sleep(5000);
            ChooseFansub();
        }

        private void ChooseFansub()
        {
            ChangeStatusMessage("Get Fansub...", Brushes.Green);
            int fansubIndex = -1;

            //Must use Regular Expression since CefSharp Evaluate "document.GetElementByClassName" return null all the time
            //string javaScriptToGetFansubName = "document.getElementsByClassName('name')";
            //string FansubContainer = monitor.b1.EvaluateScriptAsync(javaScriptToGetFansubName).Result.Message;

            //Regular Expression method
            string htmlCode = monitor.b1.GetSourceAsync().Result;
            if(htmlCode.IndexOf("<span data-translate=\"checking_browser\">") != -1)
            {
                WriteOutput("Cloud Flare don't pass, wait additional 5 seconds");
                ChangeStatusMessage("Get Fansub Failed! Retrying...", Brushes.Red);
                Thread.Sleep(5000);
                ChooseFansub();
                return;
            }
            Thread.Sleep(5000);
            Regex getFansubContainer = new Regex(@"<div class=""name""(.*?)</div>");
            MatchCollection collectionContainer = getFansubContainer.Matches(htmlCode);
            string fansubContainer = "";
            foreach (Match item in collectionContainer)
            {
                fansubContainer += item.Value;
            }
            Regex regexGetFansub = new Regex(@"<span>(.*?)</span>");
            MatchCollection fansubElements = regexGetFansub.Matches(fansubContainer);
            string[] fansubs = new string[fansubElements.Count];
            for (int i = 0; i < fansubs.Length; i++)
            {
                string output = fansubElements[i].Value.Replace("<span>", "").Replace("</span>", "");
                fansubs[i] = output;
            }
            FansubChooser chooser = new FansubChooser(fansubs);
            chooser.ClickFansub += (s, e) =>
            {
                fansubIndex = int.Parse(e.Result.ToString());
                WriteOutput(string.Format("User Returned: FansubIndex = {0}", fansubIndex));
                Dispatcher.Invoke(() => { txbFansub.Text = fansubs[fansubIndex]; });
                chooser.Close();
                GetEpisodesInSelectedFansub(fansubIndex, htmlCode);

                Thread thd = new Thread(StartDownloadAnime) { IsBackground = true, Name="MainDownloadThead"};
                thd.Start();
            };

            if (fansubs.Length < 1)
            {
                WriteOutput("Found 0 fansub, Reattempt to aquire fansub after 5 seconds");
                Thread.Sleep(5000);
                ChooseFansub();
                return;
            }
            chooser.ShowDialog();

        }

        private void GetEpisodesInSelectedFansub(int fansubIndex, string htmlCode)
        {
            //Must use Regular Expression since CefSharp Evaluate "document.GetElementByClassName" return null all the time
            //string javaScriptToGetEpsisodes = String.Format(@"document.getElementsByClassName('episodes col-lg-12 col-md-12 col-sm-12')[{0}]", fansubIndex);
            //string episodesContainer = monitor.b1.EvaluateScriptAsync(javaScriptToGetEpsisodes).Result.Message;

            //Regular Expression Method
            ChangeStatusMessage("Get All Episodes...", Brushes.Green);
            Regex getEpsidodeContainer = new Regex(@"<div class=""episodes col-lg-12 col-md-12 col-sm-12"">(.*?)</div>");
            MatchCollection matchCollEpsidodeContainer = getEpsidodeContainer.Matches(htmlCode);
            string episodesContainer = matchCollEpsidodeContainer[fansubIndex].Value;

            Regex getEPUrlsFromHtml = new Regex("href=\"(.*?)\"", RegexOptions.Singleline);
            MatchCollection collection = getEPUrlsFromHtml.Matches(episodesContainer);
            WriteOutput("Found these episodes url in this anime/ Fansub u selected: ");
            foreach (Match item in collection)
            {
                string result = item.Value.Replace("\"","").Replace("href=","");

                allEp.Add(result);
                WriteOutput(result);
            }
            WriteOutput("Finished Query Episode!");
        }

        private void StartDownloadAnime()
        {
            QueueBrowser();
            Executing();
            threadBlocker.WaitOne();
            GetRedirectedLink();
            threadBlocker2.WaitOne();
            DownloadVideo();
        }

        private void GetRedirectedLink()
        {
            Thread thread = new Thread(GetRedirectedLinkAction) { IsBackground = true, Name= "GetRedirectedLinkAction" };
            thread.Start();
        }

        private void GetRedirectedLinkAction()
        {
            foreach (var item in videoInformationUrl)
            {
                item.RedirectedVideoLink =  GetDirectedUrl(item.VideoUrl);
            }
            threadBlocker2.Set();
        }

        private void Executing()
        {
            WriteOutput("Get all video links");
            ChangeStatusMessage("Get video links: 0/" + allEp.Count, Brushes.Green);
            foreach (var item in queues)
            {
                item.Execute();
                item.Execute_Complete += GetVideo;
            }
        }

        private void GetVideo(object sender, List<VideoUrlsInformation> result)
        {
            foreach (var item in result)
            {
                videoInformationUrl.Add(item);
                WriteOutput("Video URL Added: " + item.VideoUrl);
                ChangeStatusMessage("Get video links: " + videoInformationUrl.Count + "/" + allEp.Count, Brushes.Green);
            }

            if (videoInformationUrl.Count == allEp.Count)
            {
                if(!videoInformationUrl.Any(FindGoogleAPIs))
                {
                    threadBlocker.Set();
                    WriteOutput("Get video links completed!");
                    ((QueueExecuting)sender).ExecutingThread.Abort();
                }
                else
                {
                    WriteOutput(explain);
                    ChangeStatusMessage("Found SV1 Link", Brushes.Red);
                    if(MessageBox.Show(explain, "Can't get video from old server 1 :(", MessageBoxButton.YesNo, MessageBoxImage.Error, MessageBoxResult.No) == MessageBoxResult.Yes)
                    {
                        OpenLoadDownloader();
                    }
                    else
                    {
                        WriteOutput("Download Failed :( the program will close soon!");
                        ChangeStatusMessage("Failed!", Brushes.Red);
                        new Thread(() => { Thread.Sleep(3000); Environment.Exit(-1); }).Start();
                    }
                }
            }
            ((QueueExecuting)sender).ExecutingThread.Abort();
        }

        private void OpenLoadDownloader()
        {
            MessageBox.Show("This Method not implemented yet! This will developed if the project is more well-known, The program will shutdown in 3 secs");
            WriteOutput("Download Failed :( the program will close soon!");
            ChangeStatusMessage("Failed!", Brushes.Red);
            new Thread(() => { Thread.Sleep(3000); Environment.Exit(-1); }).Start();
        }

        private bool FindGoogleAPIs(VideoUrlsInformation arg)
        {
            bool returnVal = (explain == arg.VideoUrl);
            return returnVal;

        }

        private void DownloadVideo()
        {
            string baseDownloadDir = String.Empty;
            string downloadFolder = String.Empty;
            Dispatcher.Invoke(() => { baseDownloadDir = txbDir.Text; });
            if (baseDownloadDir[baseDownloadDir.Length - 1] != '\\')
                baseDownloadDir += '\\';
            if (!Directory.Exists(baseDownloadDir))
                Directory.CreateDirectory(baseDownloadDir);
            downloadFolder = baseDownloadDir + videoInformationUrl[0].FilenameAfterDownload.Substring(0, videoInformationUrl[0].FilenameAfterDownload.IndexOf(" Ep "));
            Directory.CreateDirectory(downloadFolder);
            Thread thd = new Thread(DownloadAction);
            thd.Start(downloadFolder);
        }

        private void DownloadAction(object obj)
        {
            string downloadFolder = obj as string;
            int downloadedVideos = 0;
            foreach (var item in videoInformationUrl)
            {
                WriteOutput("Start Download: File Name: " + item.FilenameAfterDownload + "Link: " + item.RedirectedVideoLink);
                ChangeStatusMessage("Downloading: 0/" + videoInformationUrl.Count, Brushes.Green);

                WebClient web = new WebClient();
                for (int i = 0; i < 10; i++)
                {
                    try
                    {
                        web.DownloadFile(new Uri(item.RedirectedVideoLink), downloadFolder + @"\" + item.FilenameAfterDownload + ".mp4");
                        break;
                    }
                    catch
                    {
                        WriteOutput("Connection TimedOut! retrying Attemps Count: " + (i + 1).ToString() + @"/10" );
                    }
                }
                WriteOutput("Finished Download: File Name: " + item.FilenameAfterDownload + "Link: " + item.RedirectedVideoLink);
                downloadedVideos++;
                ChangeStatusMessage("Downloading: " + downloadedVideos + "/" + videoInformationUrl.Count, Brushes.Green);
            }
            WriteOutput("Download SuccessFul! The program will close automatically in 10 seconds");
            ChangeStatusMessage("Successful! ", Brushes.Green);
            Thread.Sleep(10000);
            Environment.Exit(0);
        }

        private void QueueBrowser()
        {
            WriteOutput("Add queues to make divine the work to 12 Worker Thread");
            ChangeStatusMessage("Queuing...", Brushes.Green);
            for (int i = 0; i < allEp.Count; i++)
            {
                switch (i % 12)
                {
                    case 0:
                        queues[0].AddUrl(allEp[i]);
                        break;
                    case 1:
                        queues[1].AddUrl(allEp[i]);
                        break;
                    case 2:
                        queues[2].AddUrl(allEp[i]);
                        break;
                    case 3:
                        queues[3].AddUrl(allEp[i]);
                        break;
                    case 4:
                        queues[4].AddUrl(allEp[i]);
                        break;
                    case 5:
                        queues[5].AddUrl(allEp[i]);
                        break;
                    case 6:
                        queues[6].AddUrl(allEp[i]);
                        break;
                    case 7:
                        queues[7].AddUrl(allEp[i]);
                        break;
                    case 8:
                        queues[8].AddUrl(allEp[i]);
                        break;
                    case 9:
                        queues[9].AddUrl(allEp[i]);
                        break;
                    case 10:
                        queues[10].AddUrl(allEp[i]);
                        break;
                    case 11:
                        queues[11].AddUrl(allEp[i]);
                        break;
                    default:
                        break;
                }
            }
            WriteOutput("Queue Episodes complete");
        }


        private string getElement(string element, ChromiumWebBrowser browser)
        {
            string javascript = String.Format("document.getElementById({0})", element);
            return browser.EvaluateScriptAsync(javascript).Result.Message;
        }

        private void WriteOutput(string text)
        {
            Thread thread = new Thread(() =>
            {
                Dispatcher.Invoke(() =>
                {
                    txbOutput.Text = text + "\r\n" + txbOutput.Text;
                });
            })
            { IsBackground = true, Name="OuputThread" };

            thread.Start();
        }

        private void NotifyChanged(string property)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
        }

        private void ChangeStatusMessage(string message, Brush br)
        {
            Thread thread = new Thread(() =>
            {
                Dispatcher.Invoke(() =>
                {
                    txbStatus.Text = message;
                    txbStatus.Foreground = br;
                });
            })
            { IsBackground = true, Name = "StatusThread" };

            thread.Start();
        }

        //I not write this method. Orginal Source: https://stackoverflow.com/questions/704956/getting-the-redirected-url-from-the-original-url
        public string GetDirectedUrl(string url)
        {
            HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(url);
            webRequest.AllowAutoRedirect = false;

            webRequest.Timeout = 10000;
            webRequest.Method = "HEAD";

            HttpWebResponse webResponse;
            using (webResponse = (HttpWebResponse)webRequest.GetResponse())
            {
                if ((int)webResponse.StatusCode >= 300 && (int)webResponse.StatusCode <= 399)
                    return webResponse.Headers["Location"];
            }
            return "";
        }

        private void btnDownlad_Click(object sender, RoutedEventArgs e)
        {
            if (Verfity.Text == @"Cancer Project :D")
                monitor.Show();
            else
                WriteOutput("You're not a dev or admin for this project :(");
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            ChangeStatusMessage("Checking Informaion...", Brushes.Green);
            if (txbDir.Text == string.Empty || txbURL.Text == string.Empty)
            {
                ChangeStatusMessage("Wrong Inforation", Brushes.Red);
                MessageBox.Show("Please enter all the required fields", "Unfilled Fields", MessageBoxButton.OK, MessageBoxImage.Error);
                WriteOutput("Download Canceled! Missing Url or Directory");
                return;
            }

            if (txbURL.Text.IndexOf("anime47.com/phim/") == -1)
            {
                ChangeStatusMessage("Wrong Inforation", Brushes.Red);
                MessageBox.Show("A Url must contain \"anime47.com/phim/\" because this program only design for anime47 website! ", "Invalid Url", MessageBoxButton.OK, MessageBoxImage.Error);
                WriteOutput("Download Canceled: Invalid Url - A Url must contain \"anime47.com/phim/\" because this program only design for anime47 website!");
                return;
            }

            ChangeStatusMessage("Starting...", Brushes.Green);
            AddQueues();
            btnDownload.IsEnabled = false;
            monitor.Show();
            monitor.Hide();
            getEpAsync.Start();
        }

        private void FolderChooser(object sender, RoutedEventArgs e)
        {
            //WPF don't support native SelectFolderDialog so we need to borrow from WinForms
            System.Windows.Forms.FolderBrowserDialog folderDialog = new System.Windows.Forms.FolderBrowserDialog();
            folderDialog.ShowDialog();
            txbDir.Text = folderDialog.SelectedPath;
        }

        private void StopClicked(object sender, MouseButtonEventArgs e)
        {
            player.Stop();
        }

        private void PlayClicked(object sender, MouseButtonEventArgs e)
        {
            player.PlayLooping();
        }
    }
}
