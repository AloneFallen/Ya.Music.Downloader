using Microsoft.Win32;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Xml;

namespace Ya.Music.Downloader.Yandex
{
    class Track : Music
    {
        // Данная строка собирается яндексом как соль, но при этом всегда одинакова. 
        private const string magicHash = "XGRlBW9FXlekgbPrRHuSiA";

        public string name;
        /// <summary>
        /// Продолжительность тека в мс
        /// </summary>
        public int duration;
        public string storageDir;

        int trackID;
        int albumID;

        private string fileUrl;

        public Track(int album, int track)
        {
            trackID = track;
            albumID = album;
        }
        public Track(string url) : this(Convert.ToInt32(GetIdFromUrl(url, YaType.Track)), Convert.ToInt32(GetIdFromUrl(url, YaType.Album))) { }
        public Track(JToken data)
        {
            trackID = data.SelectToken("realId").ToObject<int>();
            albumID = data.SelectToken("albums").First.SelectToken("id").ToObject<int>();
        }

        protected async override Task OnInit(JToken json = null)
        {
            if (json == null)
            {
                var url = baseUrl + "/handlers/track.jsx?track=" + albumID + "%3A" + trackID;
                string str = await web.DownloadStringTaskAsync(url);

                // На данный момент яндекс может усомниться в нас и отдаст капчу. Её нужно обработать.
                if (!TryAnswerCaptcha(ref str))
                {
                    MessageBox.Show("Вы отменили запрос на скачивание трека из-за отказа от распознавания капчи", "Внимание!", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                JToken dataSet = JObject.Parse(str);

                json = dataSet.SelectToken("track");
            }

            name = json.SelectToken("title").Value<string>();
            duration = json.SelectToken("durationMs").Value<int>();
            storageDir = json.SelectToken("storageDir").Value<string>();
            await ProcessTracksInfo();
        }

        public async override Task Download(string filename = null)
        {
            if (filename == null)
            {
                var dialog = new SaveFileDialog
                {
                    FileName = Tools.GetSafeFilename(name),
                    DefaultExt = ".mp3",
                    Filter = "Музыка (*.mp3)|*.mp3"
                };
                var result = dialog.ShowDialog();
                if (result == null || result == false)
                    return;
                filename = dialog.FileName;
            }

            await web.DownloadFileTaskAsync(fileUrl, filename);
            UpdateID3Tags(filename);
        }

        private void UpdateID3Tags(string fileName)
        {
            var file = TagLib.File.Create(fileName);
            file.Tag.Title = name;
            file.Save();
        }

        private async Task<string> GetDownloadUrl()
        {
            var str = await web.DownloadStringTaskAsync("http://storage.music.yandex.ru/download-info/" + storageDir + "/2.mp3");

            XmlDocument xml = new XmlDocument();
            xml.LoadXml(str);
            var info = xml["download-info"];

            string secret = Yandex.Tools.GetMd5Hash(magicHash + info["path"].InnerText.Substring(1) + info["s"].InnerText);
            return "http://" + info["host"].InnerText + "/get-mp3/" + secret + "/" + info["ts"].InnerText + info["path"].InnerText;
        }
        public override async Task<bool> ProcessTracksInfo()
        {
            fileUrl = await GetDownloadUrl();
            if (fileUrl == "")
                return false;

            tracks.Add(this);
            return true;
        }
    }
}
