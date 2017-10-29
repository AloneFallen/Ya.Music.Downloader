using Microsoft.Win32;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Ya.Music.Downloader.Yandex
{
    class Playlist : Music
    {
        string user;
        int id;

        public Playlist(string url)
        {
            id = Convert.ToInt32(GetIdFromUrl(url, YaType.Playlists));
            user = GetIdFromUrl(url, YaType.Users);
        }

        public async override Task Download(string filepath)
        {
            var dialog = new FolderBrowserDialog();
            var result = dialog.ShowDialog();
            if (result != true)
                return;

            filepath = dialog.SelectedPath + @"\";

            foreach (var item in tracks)
            {
                await item.Download(filepath + Tools.GetSafeFilename(item.name) + ".mp3");
            }

        }

        protected async override Task OnInit(JToken json = null)
        {
            await ProcessTracksInfo();
        }

        public override async Task<bool> ProcessTracksInfo()
        {
            var url = baseUrl + "/handlers/playlist.jsx?owner=" + user + "&kinds=" + id;
            string str = await web.DownloadStringTaskAsync(url);
            // На данный момент яндекс может усомниться в нас и отдаст капчу. Её нужно обработать.
            if (!TryAnswerCaptcha(ref str))
            {
                MessageBox.Show("Вы отменили запрос на скачивание трека из-за отказа от распознавания капчи", "Внимание!", MessageBoxButton.OK, MessageBoxImage.Information);
                return false;
            }

            // TODO: может быть добавить скачивание одновременно нескольких файлов

            JToken dataSet = JObject.Parse(str).First.First;
            var data = dataSet.SelectToken("tracks");
            foreach (var item in data)
            {
                var elem = (Track) await Music.CreateMusic(item, YaType.Track);
                elem.web = web; // чтобы пробросить коллбеки от ивентов 
                tracks.Add(elem);
            }

            return true;
        }

    }
}
