using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;

namespace LightshotParser
{
    internal class Core
    {
        static void Main(string[] args) => new Core();

        private int ThreadsCount { get; }
        private int Steps { get; }
        private bool UsingProxy { get; }
        private string ProxyIP { get; }
        private int ProxyPort { get; }

        internal Core()
        {
            string text;

            Console.Title = appData;
            if (File.Exists(lastLogsFile)) File.Delete(lastLogsFile);

            // Количество потоков
            while (true)
            {
                Console.Write("[?] Количество потоков:                  ");
                text = Console.ReadLine();

                if (int.TryParse(text, out int threadsCount))
                {
                    ThreadsCount = threadsCount;
                    break;
                }
            }

            // Количество проверок
            while (true)
            {
                Console.Write("[?] Количество проверок на каждый поток: ");
                text = Console.ReadLine();

                if (int.TryParse(text, out int steps))
                {
                    if (steps < 1) continue;
                    Steps = steps;
                    break;
                }
            }

            // Проксирование
            while (true)
            {
                Console.Write("[?] Использовать прокси? [Y/N]:          ");
                text = Console.ReadLine().ToLower();

                if (text == "y")
                {
                    UsingProxy = true;

                    // IP
                    Console.Write("[?] IP прокси:                           ");
                    ProxyIP = Console.ReadLine();

                    // Port
                    while (true)
                    {
                        Console.Write("[?] Порт прокси:                         ");
                        text = Console.ReadLine();

                        if (int.TryParse(text, out int proxyPort))
                        {
                            ProxyPort = proxyPort;
                            break;
                        }
                    }

                    break;
                }
                else if (text == "n")
                {
                    UsingProxy = false;
                    break;
                }
            }

            Console.WriteLine();
            MonitorAsync();

            // Запуск потоков
            for (int i = 0; i < ThreadsCount; i++)
            {
                Thread thread = new Thread(Cycle);
                thread.IsBackground = true;
                thread.Name = (i + 1).ToString();
                thread.Start();
                threads.Add(thread);
            }

            // Ожидаем завершения всех потоков
            foreach (var thread in threads)
            {
                thread.Join();
                ThreadsCount--;
            }

            UpdateLogs("Парсинг завершён! Нажмите любую кнопку для закрытия окна...");
            Console.ReadKey();
        }

        private readonly string appData = $"LightshotParser v0.1 [https://t.me/CSharpHive]";
        private readonly string lastLogsFile = Environment.CurrentDirectory + "/LastLogs.txt";
        private List<Thread> threads = new List<Thread>();
        private readonly object syncLog = new object();
        private readonly object syncGenerator = new object();
        private int valid, invalid;

        private void Cycle()
        {
            string thread = $"[Поток #{Thread.CurrentThread.Name}]";
            IWebDriver browser;

            UpdateLogs($"{thread} Инициализация браузера...");
            browser = InitDriver();

            for (int i = 0; i < Steps; i++)
            {
                string key = GenerateKey();
                string url = $"https://prnt.sc/{key}";

                // Переходим по ссылке
                try
                {
                    browser.Navigate().GoToUrl(url);

                    // IP забанен, завершаем поток
                    if (browser.PageSource.Contains("Access denied"))
                    {
                        UpdateLogs($"{thread} {url} IP забанен!");
                        invalid++;
                        break;
                    }
                }
                catch
                {
                    invalid++;
                    continue;
                }

                // Парсим страницу
                try
                {
                    Parse(thread, browser);
                    valid++;
                }
                catch
                {
                    UpdateLogs($"{thread} {url} Не найдено");
                    invalid++;
                    continue;
                }
            }

            browser.Quit();
        }

        private ChromeDriver InitDriver()
        {
            ChromeOptions options = new ChromeOptions();
            options.AddArguments("--headless");
            options.AddArguments("--no-sandbox");
            options.AddArguments("--disable-extensions");
            if (UsingProxy) options.AddArguments($"--proxy-server={ProxyIP}:{ProxyPort}");

            ChromeDriverService service = ChromeDriverService.CreateDefaultService();
            service.HideCommandPromptWindow = true;

            ChromeDriver browser = new ChromeDriver(service, options);
            browser.Manage().Timeouts().PageLoad = TimeSpan.FromSeconds(10);
            browser.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(10);
            return browser;
        }

        private string GenerateKey()
        {
            lock (syncGenerator)
            {
                string alphabet = "qwertyuiopasdfghjklzxcvbnm_0123456789-QWERTYUIOPASDFGHJKLZXCVBNM";
                StringBuilder sb = new StringBuilder();
                Random random = new Random();

                for (int i = 0; i < /*12*/6; i++)
                {
                    int position = random.Next(0, alphabet.Length);
                    sb.Append(alphabet[position]);
                }

                Thread.Sleep(100);
                return sb.ToString();
            }
        }

        private void Parse(string thread, IWebDriver browser)
        {
            string xpath = "//img[@id='screenshot-image']";
            string src, key;

            // Отсекаем пустые ссылки, удалённые и неактивные скриншоты
            src = browser.FindElement(By.XPath(xpath)).GetAttribute("src");
            if (src.Contains("//st.prntscr.com") || src.Contains("url=https://i.imgur.com")) throw new Exception();
            key = browser.Url.Substring(browser.Url.LastIndexOf("/") + 1);

            // Скачивание картинки
            Download(src, key);
            UpdateLogs($"{thread} {browser.Url} УСПЕХ!");
        }

        private void Download(string link, string fileName)
        {
            string folder = Environment.CurrentDirectory + "/Results";
            string extension = Path.GetExtension(link);

            using (WebClient webClient = new WebClient())
            {
                if (UsingProxy) webClient.Proxy = new WebProxy(ProxyIP, ProxyPort);
                Directory.CreateDirectory(folder);
                webClient.DownloadFile(link, $"{folder}/{fileName}{extension}");
            }
        }

        private void UpdateLogs(string message)
        {
            lock (syncLog)
            {
                message = $"[{DateTime.Now}] {message}";
                File.AppendAllText(lastLogsFile, message + "\r\n");
                Console.WriteLine(message);
            }
        }

        private async Task MonitorAsync()
        {
            while (true)
            {
                Console.Title = $"{appData} Потоков: {ThreadsCount} | Найдено: {valid} | Не найдено: {invalid}";
                await Task.Delay(100);
            }
        }
    }
}
