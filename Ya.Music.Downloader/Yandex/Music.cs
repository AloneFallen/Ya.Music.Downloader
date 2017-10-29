using HtmlAgilityPack;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Ya.Music.Downloader.Yandex
{
    abstract class Music
    {
        public WebClient web;
        public const string baseUrl = "https://music.yandex.ru";
        public static string Cookie { get; private set; }
        protected List<Track> tracks = new List<Track>(150);

        public Music()
        {
            web = new WebClient()
            {
                Encoding = Encoding.UTF8
            };
            // TODO: сохранять куки для скачки между запуском программы. Позволит избежать лишних запросов капчи
            if (Cookie != "" && Cookie != null)
                web.Headers[HttpRequestHeader.Cookie] = Cookie;
        }

        public abstract Task Download(string file = null);
        public abstract Task<bool> ProcessTracksInfo();

        public static async Task<Music> CreateMusic(string text)
        {
            var type = Tools.ParseType(text);
            Music music = null;
            switch (type)
            {
                case YaType.Track:
                    music = new Yandex.Track(text);
                    break;
                case YaType.Playlists:
                    music = new Yandex.Playlist(text);
                    break;
                case YaType.Album:
                case YaType.Unknown:
                default:
                    return null;
            }

            await music.OnInit();
            return music;
        }
        protected async static Task<Music> CreateMusic(JToken item, YaType type)
        {
            Music music = null;
            switch (type)
            {
                case YaType.Track:
                    music = new Yandex.Track(item);
                    break;
                default:
                    return null;
            }

            await music.OnInit(item);
            return music;
        }

        /// <summary>
        /// Производит дополнительную инициализацию класса после выполнения конструктора
        /// </summary>
        /// <param name="json">Json-данные необходимые для инициализации. Необязательный параметр</param>
        /// <returns></returns>
        protected async virtual Task OnInit(JToken json = null)
        {
            await Task.Run(() => { });
        }

        public static string GetIdFromUrl(string url, YaType type)
        {
            string id = "-1";
            try
            {
                var uri = new UriBuilder(url);
                var num = uri.Uri.Segments.Length;
                for (int i = 0; i < num; i++)
                {
                    var name = uri.Uri.Segments[i].Replace("/", "").ToLower();
                    var typeStr = Enum.GetName(typeof(YaType), type).ToLower();
                    if (name == typeStr)
                    {
                        id = uri.Uri.Segments[i + 1].Replace("/", "");
                        break;
                    }
                }

            }
            catch (Exception) { }
            if (id == "-1")
                throw new ArgumentException("Невозможно определить id");
            return id;
        }
        public static bool TryAnswerCaptcha(ref string str)
        {
            WebClient webClient = new WebClient()
            {
                Encoding = Encoding.UTF8
            };

            while (str.Contains("<!DOCTYPE html>") && Captcha.inCycle)
            {
                // Парсим html, чтобы найти форму с капчей и параметрами
                var postKeys = new Dictionary<string, string>();
                string result = str;
                var doc = new HtmlDocument();
                doc.LoadHtml(str);
                var form = doc.DocumentNode.Descendants("form").First();
                var imgUrl = form.Descendants("img").First().GetAttributeValue("src", "");
                var inputs = form.Descendants("input");
                foreach (var item in inputs)
                {
                    postKeys.Add(item.GetAttributeValue("name", ""), WebUtility.HtmlDecode(item.GetAttributeValue("value", "")));
                }

                // Скачиваем картинку с капчей чтобы отобразить в форме
                var captchaImg = webClient.DownloadData(imgUrl);
               
                // Отображаем форму с капчей и ждем ответа от пользователя.
                // TODO: можно добавить сервис по автораспознаванию
                Captcha.WinCaptcha.Dispatcher.Invoke(() =>
                {
                    // Инциализация картинки для формы должна быть в том же потоке что и UI формы
                    BitmapImage biImg = new BitmapImage();
                    MemoryStream ms = new MemoryStream(captchaImg);
                    biImg.BeginInit();
                    biImg.StreamSource = ms;
                    biImg.EndInit();
                    Captcha.WinCaptcha.captchaImage.Source = biImg as ImageSource;
                    Captcha.WinCaptcha.ShowDialog();
                });
                
                // Подгатавливаем параметры для ответа на капчу
                string url = "https://music.yandex.ru" + form.GetAttributeValue("action", "/checkcaptcha");
                string param = string.Join("&", postKeys.Select((kvp) =>
                {
                    if (kvp.Value == "")
                        return kvp.Key + "=" + WebUtility.UrlEncode(Captcha.answer);
                    return kvp.Key + "=" + WebUtility.UrlEncode(kvp.Value);
                }));
                url += "?" + param;

                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                request.CookieContainer = new CookieContainer();
                request.AllowAutoRedirect = false; // нам не нужно переходить по редиректу
                try
                {
                    HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                    string redirUrl = response.Headers["Location"];
                    response.Close();

                    if (response.Cookies.Count > 0 && redirUrl != null)
                    {
                        webClient.Headers[HttpRequestHeader.Cookie] = response.Cookies[0].ToString();
                        result = webClient.DownloadString(redirUrl);
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.Message);
                }

                Cookie = webClient.Headers[HttpRequestHeader.Cookie];
                str = result;
            }

            var retVal = Captcha.inCycle;
            Captcha.Reset();

            return retVal;
        }


    }
}
