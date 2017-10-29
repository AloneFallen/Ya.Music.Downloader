using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Ya.Music.Downloader
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            statusLabel.Foreground = Brushes.Black;

            if (!editUrl.Text.Contains("music.yandex.ru"))
            {
                MessageBox.Show("Неверная ссылка для парсинга", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var music = await Yandex.Music.CreateMusic(editUrl.Text);
            if(music == null)
            {
                MessageBox.Show("Указанная ссылка не ведет к музыкальным файлам", "Ошибка!", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return;
            }
            music.web.DownloadFileCompleted += DownloadProgressFinished;
            music.web.DownloadProgressChanged += DownloadProgressChanged;

            await music.Download();
            statusLabel.Content = "Скачивание завершено";
            statusLabel.Foreground = Brushes.Green;
        }

        private void DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            var client = (WebClient)sender;
            var header = client.ResponseHeaders[ HttpResponseHeader.ContentType];
            if (!header.Contains("audio"))
                return;

            statusLabel.Dispatcher.Invoke(() =>
            {
                statusLabel.Content = e.ProgressPercentage.ToString() + " %";  // "\n" + "Файлов: "+ 0 + "из " + 10;
            });
            
        }

        private void DownloadProgressFinished(object send, AsyncCompletedEventArgs ev)
        {

        }
}
}
