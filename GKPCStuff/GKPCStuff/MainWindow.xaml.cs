using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.IO;
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
using System.Windows.Threading;

namespace GKPCStuff
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        DispatcherTimer timer;
        private bool berem = false;
        public string computerName = "";

        public MainWindow()
        {
            InitializeComponent();
        }

        private void ZapisiLog(string value)
        {
            lbIzpis.Items.Add(value);
            lbIzpis.Items.MoveCurrentToLast();
            lbIzpis.ScrollIntoView(lbIzpis.Items.CurrentItem);
        }

        private void button_Click(object sender, RoutedEventArgs e)
        {
            if (berem == false)
            {
                btnPovezi.IsEnabled = false;

                ZapisiLog("Pričenjam brati ...");

                computerName = Environment.MachineName;
                ZapisiLog("Ime računalnika: " + computerName);

                timer = new DispatcherTimer();
                timer.Tick += Timer_Tick;
                timer.Interval = new TimeSpan(0, 0, Int32.Parse(tbRazmikProzenja.Text));
                timer.Start();

                berem = true;
                btnPovezi.Content = "Končaj brati";

                Configuration config = ConfigurationManager.OpenExeConfiguration(System.Reflection.Assembly.GetExecutingAssembly().Location);
                
                if (config.AppSettings.Settings["tbRazmikProzenja"] == null)
                    config.AppSettings.Settings.Add("tbRazmikProzenja", tbRazmikProzenja.Text);
                else
                    config.AppSettings.Settings["tbRazmikProzenja"].Value = tbRazmikProzenja.Text;

                if (config.AppSettings.Settings["tbAPIUrl"] == null)
                    config.AppSettings.Settings.Add("tbAPIUrl", tbAPIUrl.Text);
                else
                    config.AppSettings.Settings["tbAPIUrl"].Value = tbAPIUrl.Text;

                if (config.AppSettings.Settings["tbStrukturaPodatkov"] == null)
                    config.AppSettings.Settings.Add("tbStrukturaPodatkov", tbStrukturaPodatkov.Text);
                else
                    config.AppSettings.Settings["tbStrukturaPodatkov"].Value = tbStrukturaPodatkov.Text;
              
                config.Save(ConfigurationSaveMode.Minimal);

                btnPovezi.IsEnabled = true;
            }
            else
            {
                timer.Stop();

                ZapisiLog("Končal sem brati ...");

                berem = false;
                btnPovezi.Content = "Poveži in beri";
            }
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            try
            {
                double cpuTemp = 0;
                try
                {
                    var CPUTemps = CPUTemperature.Temperatures;
                    cpuTemp = CPUTemps[0].CurrentValue;
                }
                catch { }
                ZapisiLog("CPU Temp (Celsius): " + cpuTemp);

                double cpuLoad = 0;
                try
                {
                    var cpuLoads = CPULoad.Loads;
                    cpuLoad = cpuLoads[0].PercentProcessorTime;
                }
                catch { }
                ZapisiLog("CPU Load (%): " + cpuLoad);

                double memoryLoad = 0;
                try
                {
                    var memoryLoads = MemoryLoad.Loads;
                    memoryLoad = memoryLoads[0].Load;
                }
                catch { }
                ZapisiLog("Memory Load (%): " + memoryLoad);

                double nrOfProc = 0;
                double nrOfThread = 0;
                try
                {
                    var ops = FileOperations.Datas;
                    nrOfProc = ops[0].Processes;
                    nrOfThread = ops[0].Threads;
                }
                catch { }
                ZapisiLog("Nr. Of Processes: " + nrOfProc);
                ZapisiLog("Nr. Of Threads: " + nrOfThread);

                PosljiVPBI(cpuTemp, cpuLoad, memoryLoad, nrOfProc, nrOfThread);
            }
            catch (Exception _ex)
            {
                ZapisiLog("NAPAKA: " + _ex.Message);
                ZapisiLog(_ex.StackTrace);
            }
        }

        private void PosljiVPBI(double cpuTemp, double cpuLoad, double memoryLoad, double nrOfProc, double nrOfTread)
        {
            try
            {
                if ((!String.IsNullOrEmpty(tbAPIUrl.Text)) && (!String.IsNullOrEmpty(tbStrukturaPodatkov.Text)))
                {
                    string json = tbStrukturaPodatkov.Text;
                    if (json.StartsWith("["))
                        json = json.Substring(1, json.Length - 1);
                    if (json.EndsWith("]"))
                        json = json.Substring(0, json.Length - 1);

                    JObject jsonObj = JObject.Parse(json);
                    jsonObj["TimeStamp"] = DateTime.UtcNow.ToString("s") + "Z";
                    jsonObj["ComputerName"] = computerName;
                    jsonObj["CPU Temp"] = cpuTemp.ToString(new CultureInfo("en-US"));
                    jsonObj["CPU Load"] = cpuLoad.ToString(new CultureInfo("en-US"));
                    jsonObj["Memory Load"] = memoryLoad.ToString(new CultureInfo("en-US"));
                    jsonObj["NrOfThreads"] = nrOfTread.ToString(new CultureInfo("en-US"));
                    jsonObj["NrOfProcessess"] = nrOfProc.ToString(new CultureInfo("en-US"));

                    json = "[" + JsonConvert.SerializeObject(jsonObj) + "]";

                    //create an HTTP request to the URL that we need to invoke
                    HttpWebRequest request = (HttpWebRequest)WebRequest.Create(tbAPIUrl.Text);
                    request.ContentType = "application/json; charset=utf-8"; //set the content type to JSON
                    request.Method = "POST"; //make an HTTP POST

                    using (var streamWriter = new StreamWriter(request.GetRequestStream()))
                    {
                        streamWriter.Write(json);
                        streamWriter.Flush();
                        streamWriter.Close();
                    }

                    WebResponse response = request.GetResponse();
                    var streamReader = new StreamReader(response.GetResponseStream());
                    var result = streamReader.ReadToEnd();
                    ZapisiLog(result);
                }
            }
            catch (Exception _ex)
            {
                ZapisiLog("NAPAKA PowerBI: " + _ex.Message);
                ZapisiLog(_ex.StackTrace);
            }
        }

        private void btnPobrisi_Click(object sender, RoutedEventArgs e)
        {
            lbIzpis.Items.Clear();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Configuration config = ConfigurationManager.OpenExeConfiguration(System.Reflection.Assembly.GetExecutingAssembly().Location);

            try
            {
                tbRazmikProzenja.Text = config.AppSettings.Settings["tbRazmikProzenja"].Value;
                tbAPIUrl.Text = config.AppSettings.Settings["tbAPIUrl"].Value;
                tbStrukturaPodatkov.Text = config.AppSettings.Settings["tbStrukturaPodatkov"].Value;
            }
            catch { }
        }
    }
}
