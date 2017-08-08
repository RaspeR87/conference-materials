using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.IO;
using System.IO.Ports;
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

namespace GKMicroBit
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private bool berem = false;
        private SerialPort mySerialPort;

        public MainWindow()
        {
            InitializeComponent();
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
                tbComPort.Text = config.AppSettings.Settings["tbComPort"].Value;
                tbAPIUrl.Text = config.AppSettings.Settings["tbAPIUrl"].Value;
                tbStrukturaPodatkov.Text = config.AppSettings.Settings["tbStrukturaPodatkov"].Value;
            }
            catch { }
        }

        private void btnPovezi_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (berem == false)
                {
                    ZapisiLog("Pričenjam brati ...");

                    berem = true;
                    btnPovezi.Content = "Končaj brati";

                    mySerialPort = new SerialPort(tbComPort.Text);

                    mySerialPort.BaudRate = 115200;
                    mySerialPort.Parity = Parity.None;
                    mySerialPort.StopBits = StopBits.One;
                    mySerialPort.DataBits = 8;
                    mySerialPort.Handshake = Handshake.None;
                    mySerialPort.RtsEnable = true;

                    mySerialPort.DataReceived += new SerialDataReceivedEventHandler(DataReceivedHandler);

                    mySerialPort.Open();

                    Configuration config = ConfigurationManager.OpenExeConfiguration(System.Reflection.Assembly.GetExecutingAssembly().Location);

                    if (config.AppSettings.Settings["tbComPort"] == null)
                        config.AppSettings.Settings.Add("tbComPort", tbComPort.Text);
                    else
                        config.AppSettings.Settings["tbComPort"].Value = tbComPort.Text;

                    if (config.AppSettings.Settings["tbAPIUrl"] == null)
                        config.AppSettings.Settings.Add("tbAPIUrl", tbAPIUrl.Text);
                    else
                        config.AppSettings.Settings["tbAPIUrl"].Value = tbAPIUrl.Text;

                    if (config.AppSettings.Settings["tbStrukturaPodatkov"] == null)
                        config.AppSettings.Settings.Add("tbStrukturaPodatkov", tbStrukturaPodatkov.Text);
                    else
                        config.AppSettings.Settings["tbStrukturaPodatkov"].Value = tbStrukturaPodatkov.Text;
                    
                    config.Save(ConfigurationSaveMode.Minimal);
                }
                else
                {
                    mySerialPort.Close();

                    ZapisiLog("Končal sem brati ...");

                    berem = false;
                    btnPovezi.Content = "Poveži in beri";
                }
            }
            catch { }
        }

        private void PosljiVPBI(double temperatura, double svetloba, double steviloKlikov, double upTime)
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
                    jsonObj["Timestamp"] = DateTime.UtcNow.AddHours(2).ToString("s") + "Z";
                    jsonObj["Temperatura"] = temperatura.ToString(new CultureInfo("en-US"));
                    jsonObj["Svetloba"] = svetloba.ToString(new CultureInfo("en-US"));
                    jsonObj["SteviloKlikov"] = steviloKlikov.ToString(new CultureInfo("en-US"));
                    jsonObj["UpTime"] = (upTime / 1000).ToString(new CultureInfo("en-US"));

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

        private void ZapisiLog(string value)
        {
            lbIzpis.Items.Add(value);
            lbIzpis.Items.MoveCurrentToLast();
            lbIzpis.ScrollIntoView(lbIzpis.Items.CurrentItem);
        }

        private void DataReceivedHandler(object sender, SerialDataReceivedEventArgs e)
        {
            try
            {
                SerialPort sp = (SerialPort)sender;
                string indata = sp.ReadLine();

                //indata = "Temp:31,13,5,958802\r";

                indata = indata.Replace("\r", "").Replace("\n", "");
                string[] indatas = indata.Split(',');

                this.Dispatcher.Invoke(() =>
                {
                    lbIzpis.Items.Add(indata);

                    string[] temps = indatas[0].Split(':');

                    PosljiVPBI(Double.Parse(temps[1]), Double.Parse(indatas[1]), Double.Parse(indatas[2]), Double.Parse(indatas[3]));
                });
            }
            catch { }
        }
    }
}
