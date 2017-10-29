using System;
using System.Collections.Generic;
using System.Diagnostics;
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
using System.Windows.Shapes;

namespace Ya.Music.Downloader
{
    /// <summary>
    /// Interaction logic for Captcha.xaml
    /// </summary>
    public partial class Captcha : Window
    {
        // Позволяет показывать окно снова
        public static bool inCycle = true;
        public static string answer = "";
        private static Captcha _winCaptcha = null;
        public static Captcha WinCaptcha
        {
            get
            {
                if (_winCaptcha == null)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        // Установим значения по умолчанию после закрытия окна, при первой инициализации
                        Reset();
                        _winCaptcha = new Captcha();
                    });
                }
                return _winCaptcha;
            }
        }

        public Captcha()
        {
            InitializeComponent();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // Выполняется только если окно закрыто по конпке отправить
            if (!inCycle)
                inCycle = true;
            else inCycle = false;  // Если мы закрыли форму сами, т.е не хотим вводить капчу

            _winCaptcha = null;
        }

        private void CaptchaSubmit_Click(object sender, RoutedEventArgs e)
        {
            answer = captchaTextBox.Text;

            // Необходимо закрывать форму и показывать снова если капча неверна
            inCycle = false;
            Close();
        }
        
        public static void Reset()
        {
            answer = "";
            inCycle = true;
            _winCaptcha = null;
        }

        private void captchaTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {

        }
    }
}
