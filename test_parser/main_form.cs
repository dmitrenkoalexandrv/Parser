using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net;
using System.Web;
using System.IO;
using HtmlAgilityPack;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using Newtonsoft.Json.Linq;
using System.Threading;

namespace test_parser
{
    public partial class f_main : Form
    {
        // Первая страница
        private int first_page;
        // Последняя страница
        private int last_page;
        // Используется при добавке параметра к URL
        private char param_separator = '?';
        // Домен
        private const string main_url = "https://uk.wikipedia.org/wiki/%D0%92%D1%96%D0%BA%D1%96%D0%BF%D0%B5%D0%B4%D1%96%D1%8F:%D0%92%D1%96%D0%BA%D1%96_%D0%BB%D1%8E%D0%B1%D0%B8%D1%82%D1%8C_%D0%BF%D0%B0%D0%BC%27%D1%8F%D1%82%D0%BA%D0%B8/%D0%9A%D0%B8%D1%97%D0%B2/%D0%A8%D0%B5%D0%B2%D1%87%D0%B5%D0%BD%D0%BA%D1%96%D0%B2%D1%81%D1%8C%D0%BA%D0%B8%D0%B9_%D1%80%D0%B0%D0%B9%D0%BE%D0%BD_(%D0%A7%E2%80%93%D0%AF)";
        // Потокобезопасная запись данных в RTB
        public delegate void add_text(string str);
        public add_text my_delegate;

        // Событие изменения таскбара
        public event Action<int> ProgressChanged;

        // Токен для отмены задачи
        private CancellationTokenSource _tokenSource;


        /// <summary>
        /// Конструктор
        /// </summary>
        public f_main()
        {
            InitializeComponent();
            Uri uri = new Uri(main_url);
            tb_url.Text = uri.LocalPath;
            tb_first_page.Text = "1";
            tb_last_page.Text = "1";
            toolStripStatusLabel1.Text = "0 %";
        }

        // Метод сообщенный с делегатом my_delegate
        public void add_text_method(string str)
        {
            rtb_output.Text += str;
        }

        /// <summary>
        /// Отправка GET-запроса на сервер
        /// </summary>
        /// <param name="url">Запрашиваемый URL</param>
        /// <returns>Код страницы (или пустая строка в случае ошибки)</returns>
        public string getRequest(string url) 
        { 
            try 
            { 
                var httpWebRequest = (HttpWebRequest) WebRequest.Create(url); 
                httpWebRequest.AllowAutoRedirect = false;       //Запрещаем автоматический реддирект 
                httpWebRequest.Method = "GET";                  //Можно не указывать, по умолчанию используется GET. 
                httpWebRequest.Referer = "http://google.com";   //Реферер. Тут можно указать любой URL
                using (var httpWebResponse = (HttpWebResponse) httpWebRequest.GetResponse()) 
                { 
                    using (var stream = httpWebResponse.GetResponseStream()) 
                    { 
                        using (var reader = new StreamReader(stream, Encoding.GetEncoding(httpWebResponse.CharacterSet))) 
                        { 
                            return reader.ReadToEnd(); 
                        } 
                    } 
                } 
            } 
            catch 
            {
                return String.Empty;
            } 
        }

        // Load images with specific markup 1
        public static string Load_Image1(string filename,string name,string id)
        {
            PictureBox Image1 = new PictureBox();
            Label Image1_title = new Label();

            string Jpeg = @"c:\tmp\";

            using (WebClient wc = new WebClient())
            {
                var client = new WebClient();
                var uri = ("https://en.wikipedia.org/w/api.php?action=query&prop=imageinfo&iiprop=comment|url|dimensions&format=json&iiurlwidth=400&titles=File:" + name);
                var response = client.DownloadString(new Uri(uri));
                JObject obj = JObject.Parse(response);

                if (obj["query"]["pages"][id]["imageinfo"] != null)
                {
                    string image1 = (string)obj["query"]["pages"][id]["imageinfo"][0]["url"];
                    Image1.SizeMode = PictureBoxSizeMode.StretchImage;
                    Image1.LoadAsync(image1);

                    string image1_Title = (string)obj["query"]["pages"][id]["title"];
                    Image1_title.Text = image1_Title;

                    var hash = uri.GetHashCode();
                    //var path = Path.Combine(Jpeg, hash.ToString("X") + ".jpg");
                    var path = Path.Combine(Jpeg, filename + ".jpg");
                    client.DownloadFile(image1, path);

                    return (string)obj["query"]["pages"][id]["imageinfo"];
                }
                else
                {
                    return "-1";
                }
            }
        }

        // Load images with specific markup reserve
        public static void Load_Image1_reserve(string filename, string name, string id)
        {
            PictureBox Image1 = new PictureBox();
            Label Image1_title = new Label();

            string Jpeg = @"c:\tmp\";

            using (WebClient wc = new WebClient())
            {
                var client = new WebClient();
                var uri = ("https://en.wikipedia.org/w/api.php?action=query&prop=imageinfo&iiprop=comment|url|dimensions&format=json&iiurlwidth=400&titles=File:" + name);
                var response = client.DownloadString(new Uri(uri));
                JObject obj = JObject.Parse(response);

                string image1 = name;
                Image1.SizeMode = PictureBoxSizeMode.StretchImage;
                Image1.LoadAsync(image1);

                string image1_Title = (string)obj["query"]["pages"][id]["title"];
                Image1_title.Text = image1_Title;

                var hash = uri.GetHashCode();
                var path = Path.Combine(Jpeg, filename + ".jpg");
                try
                {
                    client.DownloadFile(image1, path);
                }
                catch
                {
                    client.DownloadFile(image1.Replace("6400px", "800px"), path);
                }
            }
        }

        /// <summary>
        /// Основной метод парсинга. Вызывается в отдельном потоке.
        /// </summary>
        public void start(HtmlAgilityPack.HtmlDocument doc, int cnt, CancellationToken token)
        {
            byte ascii = 10;
            char ch = (char)ascii;
            my_delegate = new add_text(add_text_method);
            
            for (int j = 2; j < cnt + 1; j++)
            {
                token.ThrowIfCancellationRequested();

                string c4Value;
                string c5Value = "";
                string c8Value = "";
                string c88Value = "";
                string res = "";

                HtmlNodeCollection c = doc.DocumentNode.SelectNodes("//table[@class='wikitable sortable']/tr[" + @j + "]/td[1]");
                HtmlNodeCollection c1 = doc.DocumentNode.SelectNodes("//table[@class='wikitable sortable']/tr[" + @j + "]/td[2]");
                HtmlNodeCollection c2 = doc.DocumentNode.SelectNodes("//table[@class='wikitable sortable']/tr[" + @j + "]/td[3]");
                HtmlNodeCollection c3 = doc.DocumentNode.SelectNodes("//table[@class='wikitable sortable']/tr[" + @j + "]/td[4]");
                HtmlNode c4 = doc.DocumentNode.SelectSingleNode("//table[@class='wikitable sortable']//tr[" + @j + "]//td[5]");
                HtmlNodeCollection c5 = doc.DocumentNode.SelectNodes("//table[@class='wikitable sortable']/tr[" + @j + "]/td[6]/span[1]");
                HtmlNodeCollection c6 = doc.DocumentNode.SelectNodes("//table[@class='wikitable sortable']/tr[" + @j + "]/td[7]");
                HtmlNodeCollection c8 = doc.DocumentNode.SelectNodes("//table[@class='wikitable sortable']/tr[" + @j + "]/td[9]/a[1]/img[1]");

                if (c4 == null)
                {
                    c4Value = "";
                }
                else
                {
                    try
                    { 
                        c4Value = c4.InnerText.Split(ch)[2]; 
                    }
                    catch
                    {
                        c4Value = c4.InnerText; 
                    }
                }

                if (c5 == null)
                {
                    c5Value = "";
                }
                else
                {
                    c5Value = c5[0].InnerText;
                }

                if (c8 == null)
                {
                    c8Value = "";
                    c88Value = "";
                }
                else
                {
                    c8Value = c8[0].Attributes["src"].Value;
                    c88Value = c8[0].Attributes["alt"].Value;
                }

                rtb_output.Invoke(my_delegate, new object[] { c[0].InnerText.Trim() + ";" + c1[0].InnerText.Trim().Replace("&#160;", " ").Replace(";", "|") + ";" + c2[0].InnerText.Trim().Replace(";", "|") + ";" + c3[0].InnerText.Trim() + ";" + c4Value.Replace("&#160;", " ") + ";" + c5Value.Trim() + ";" + c6[0].InnerText.Trim().Replace("&#160;", "") + ";" + c88Value + "\n" });

                if (c8Value != "")
                {
                    res = Load_Image1(c[0].InnerText.Trim(), c8Value, "-1");

                    if (res == "-1")
                    {
                       Load_Image1_reserve(c[0].InnerText.Trim(), "https:" + c8Value.Replace("180px", "6400px"), "-1");
                    }
                }

                // Запускаем событие
                ProgressChanged(j);
            }
        }


        // Обработка нажатия кнопки Go!
        private async void b_go_Click(object sender, EventArgs e)
        {
            string message = "";
            Task task = null;
            // Проверка корректности номеров страниц
            this.first_page = string2int(tb_first_page.Text);
            if (this.first_page == -1 || this.first_page == 0) return; // -1 - если в поле не число. 0 - если там 0 (а мы работаем с первой страницы)
            this.last_page = string2int(tb_last_page.Text);
            if (this.last_page == -1) return;
            if (this.last_page < this.first_page) return;
            // Нормальную проверку корректности url не делал ибо влом
            if (tb_url.Text == String.Empty) return;
            if (tb_url.Text.IndexOf('?') > 0) this.param_separator = '&';

            // Инициализация контролов
            toolStripProgressBar1.Value = 1;
            toolStripProgressBar1.Minimum = 1;
            toolStripStatusLabel1.Text = "0 %";
            rtb_output.Clear();
            rtb_output.Enabled = false;
            b_go.Enabled = false;

            // Получаем html документ
            string content = getRequest(main_url);
            HtmlAgilityPack.HtmlDocument doc = new HtmlAgilityPack.HtmlDocument();
            doc.LoadHtml(content);

            // Получаем количество записей в таблице на веб-странице
            tb_last_page.Text = doc.DocumentNode.SelectNodes("//table[@class='wikitable sortable']/tr").Count.ToString();
            toolStripProgressBar1.Maximum = Int32.Parse(tb_last_page.Text);

            // Подписываемся на событие
            ProgressChanged += ProgressChangeFunction;

            // Инит токена отмены
            _tokenSource = new CancellationTokenSource();
            CancellationToken token = _tokenSource.Token;

            // Запуск метода получения информации\фото в другом потоке
            try
            {
                task = Task.Factory.StartNew(() => this.start(doc, toolStripProgressBar1.Maximum, token), token);
                await task;
            }
            catch (OperationCanceledException)
            {
                
            }

            // Делаем элементы формы активными
            rtb_output.Enabled = true;
            b_go.Enabled = true;
            b_stop.Enabled = true;
            
            message = task.IsCanceled ?  "Операция отменена!" : "Операция завершена!";
            MessageBox.Show(message,"Информация", MessageBoxButtons.OK,MessageBoxIcon.Information);
        }

        // обработка клика кнопки Stop!
        private void b_stop_Click(object sender, EventArgs e)
        {
            b_stop.Enabled = false;
            _tokenSource.Cancel();
        }

        // функция обработки для события изменения прогрессбара и label с процентами
        private void ProgressChangeFunction(int progress)
        {
            Action action = () =>
                {
                    toolStripProgressBar1.Value = progress;
                    toolStripStatusLabel1.Text = Math.Round((((progress) / Double.Parse(tb_last_page.Text)) * 100.00), 2).ToString() + " %";
                };
            // Без Invoke и делегата получаем ошибку обращения к контролам из другого потока (похоже что он вызывается в потоке основной формы)
            Invoke(action);
        }


        /// <summary>
        /// Убираем повторы из массива строк
        /// </summary>
        /// <param name="input">Исходний массив</param>
        /// <returns>Отфильтрованный массив</returns>
        private string[] array_unique(string[] input)
        {
            Dictionary<int, string> tmp = new Dictionary<int, string>();
            for (int i = 0; i < input.Length; i++)
            {
                if (get_key_by_val(tmp, input[i]) == -1)
                {
                    tmp.Add(i, input[i]);
                }
            }
            string[] output = new string[tmp.Count];
            int j = 0;
            foreach (string v in tmp.Values)
            {
                output[j] = v;
                j++;
            }
            return output;
        }
        /// <summary>
        /// Возвращает ключ в словаре по его значению. Если не найдено, то вернет -1
        /// </summary>
        /// <param name="d">Словарь</param>
        /// <param name="v">Значение, ключ которого ищется</param>
        /// <returns></returns>
        private int get_key_by_val(Dictionary<int, string> d, string v)
        {
            foreach (int k in d.Keys)
            {
                if (d[k] == v) return k;
            }
            return -1;
        }
        /// <summary>
        /// Перевод строки в число
        /// </summary>
        /// <param name="str">Переводимая строка</param>
        /// <returns>Число</returns>
        private int string2int(string str)
        {
            try
            {
                int i = Convert.ToInt16(str);
                if (i <= 0) return -1;
                else return i;
            }
            catch { return -1; }
        }

        private void f_main_Load(object sender, EventArgs e)
        {
            Action action = () =>
            {
                while (true)
                {
                    Invoke((Action) (() => label3.Text = DateTime.Now.ToLongTimeString()));
                    Thread.Sleep(1000);
                }
            };

            Task task = Task.Factory.StartNew(action);                
        }



    }
}
