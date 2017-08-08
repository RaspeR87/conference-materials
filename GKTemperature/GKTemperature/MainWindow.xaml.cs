using System;
using System.Collections.Generic;
using System.Linq;
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

using DalSemi.OneWire;
using DalSemi.OneWire.Container;
using DalSemi.OneWire.Adapter;
using System.Windows.Threading;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Net;
using System.IO;
using System.Device.Location;
using System.Xml.Linq;
using System.Globalization;
using System.Threading;
using System.Configuration;

namespace GKTemperature
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        PortAdapter adapter;
        DispatcherTimer timer;
        private bool berem = false;
        private string lokacijaStr = "";
        private string drzavaStr = "";

        enum Komande : byte
        {
            SkipRom = 0xCC,
            MeasureTemperature = 0x44,
            MatchRom = 0x55,
            ReadRom = 0xBE
        }

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

        public string[] DobiNaslovIzGMaps(string lat, string lng)
        {
            string gmapsUri = "http://maps.googleapis.com/maps/api/geocode/xml?latlng={0},{1}&sensor=false";
            string[] returnV = new string[2];
            returnV[0] = "";
            returnV[1] = "";

            try
            {
                string requestUri = string.Format(gmapsUri, lat, lng);

                using (WebClient wc = new WebClient())
                {
                    string result = wc.DownloadString(requestUri);
                    var xmlElm = XElement.Parse(result);
                    var status = (from elm in xmlElm.Descendants()
                                  where elm.Name == "status"
                                  select elm).FirstOrDefault();

                    if (status.Value.ToLower() == "ok")
                    {
                        var lokStr = "";
                        try
                        {
                            lokStr = xmlElm.Descendants("address_component")
                              .Where(x => x.Descendants("type").Any(y => y.Value == "locality")).First().Descendants().Where(x => x.Name == "long_name").First().Value;
                        }
                        catch { }

                        var drzStr = "";
                        try
                        {
                            drzStr = xmlElm.Descendants("address_component")
                              .Where(x => x.Descendants("type").Any(y => y.Value == "country")).First().Descendants().Where(x => x.Name == "long_name").First().Value;
                        }
                        catch { }

                        returnV[0] = lokStr;
                        returnV[1] = drzStr;
                    }
                }
            }
            catch (Exception _ex)
            {
                ZapisiLog("NAPAKA GMaps: " + _ex.Message);
                ZapisiLog(_ex.StackTrace);
            }

            return returnV;
        }

        private void btnPovezi_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (berem == false)
                {
                    if ((String.IsNullOrEmpty(tbComPort.Text)) || (String.IsNullOrEmpty(tbRazmikProzenja.Text)))
                    {
                        MessageBox.Show("Potrebno je vnesti COM port in razmik proženja!");
                        return;
                    }

                    GeoCoordinateWatcher lokacija = new GeoCoordinateWatcher();
                    int stPonovitev = 0;
                    while (stPonovitev <= 10)
                    {
                        lokacija.TryStart(false, TimeSpan.FromMilliseconds(10000));
                        GeoCoordinate koordinate = lokacija.Position.Location;
                        if (koordinate.IsUnknown != true)
                        {
                            var latitude = koordinate.Latitude;
                            var longitude = koordinate.Longitude;

                            string[] lokacijaArray = DobiNaslovIzGMaps(latitude.ToString(new CultureInfo("en-US")), longitude.ToString(new CultureInfo("en-US")));
                            lokacijaStr = lokacijaArray[0];
                            drzavaStr = lokacijaArray[1];

                            break;
                        }

                        Thread.Sleep(2000);
                        stPonovitev++;
                    }

                    adapter = AccessProvider.GetAdapter("{DS9097}", tbComPort.Text);
                    OneWireContainer container = adapter.GetDeviceContainer(StringToByteArray("28FF945B521604BF"));
                    string owcType = container.GetType().ToString();
                    if (owcType.Equals("DalSemi.OneWire.Container.OneWireContainer28"))
                    {
                        // imamo DS18S20
                        adapter.SelectDevice(container.Address);

                        ZapisiLog("Pričenjam brati ...");

                        timer = new DispatcherTimer();
                        timer.Tick += Timer_Tick;
                        timer.Interval = new TimeSpan(0, 0, Int32.Parse(tbRazmikProzenja.Text));
                        timer.Start();

                        berem = true;
                        btnPovezi.Content = "Končaj brati";
                    }

                    Configuration config = ConfigurationManager.OpenExeConfiguration(System.Reflection.Assembly.GetExecutingAssembly().Location);

                    if (config.AppSettings.Settings["tbComPort"] == null)
                        config.AppSettings.Settings.Add("tbComPort", tbComPort.Text);
                    else
                        config.AppSettings.Settings["tbComPort"].Value = tbComPort.Text;

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

                    if (config.AppSettings.Settings["tbMin"] == null)
                        config.AppSettings.Settings.Add("tbMin", tbMin.Text);
                    else
                        config.AppSettings.Settings["tbMin"].Value = tbMin.Text;

                    if (config.AppSettings.Settings["tbMax"] == null)
                        config.AppSettings.Settings.Add("tbMax", tbMax.Text);
                    else
                        config.AppSettings.Settings["tbMax"].Value = tbMax.Text;

                    if (config.AppSettings.Settings["tbTarget"] == null)
                        config.AppSettings.Settings.Add("tbTarget", tbTarget.Text);
                    else
                        config.AppSettings.Settings["tbTarget"].Value = tbTarget.Text;

                    config.Save(ConfigurationSaveMode.Minimal);
                }
                else
                {
                    timer.Stop();

                    ZapisiLog("Končal sem brati ...");

                    berem = false;
                    btnPovezi.Content = "Poveži in beri";
                }
            }
            catch (Exception _ex)
            {
                ZapisiLog("NAPAKA: " + _ex.Message);
                ZapisiLog(_ex.StackTrace);
            }
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            try
            {
                adapter.Reset();

                adapter.PutByte((int)Komande.SkipRom);
                adapter.PutByte((int)Komande.MeasureTemperature);

                adapter.Reset();

                adapter.PutByte((int)Komande.SkipRom);
                adapter.PutByte((int)Komande.ReadRom);

                int rawTemperature = (UInt16)((UInt16)adapter.GetByte() | (UInt16)(adapter.GetByte() << 8));
                double result = 1;
                if ((rawTemperature & 0x8000) > 0)
                {
                    rawTemperature = (rawTemperature ^ 0xffff) + 1;
                    result = -1;
                }
                result *= (6 * rawTemperature + rawTemperature / 4.0) / 100.0;

                ZapisiLog(result.ToString("N2") + " °C");
                PosljiVPBI(result);
            }
            catch (Exception _ex)
            {
                ZapisiLog("NAPAKA: " + _ex.Message);
                ZapisiLog(_ex.StackTrace);
            }
        }

        private void PosljiVPBI(double temperature)
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
                    jsonObj["Timestamp"] = DateTime.UtcNow.ToString("s") + "Z";
                    jsonObj["Temperature"] = temperature.ToString(new CultureInfo("en-US"));
                    jsonObj["City"] = lokacijaStr;
                    jsonObj["Country"] = drzavaStr;
                    jsonObj["Minimum"] = Double.Parse(tbMin.Text).ToString(new CultureInfo("en-US"));
                    jsonObj["Maximum"] = Double.Parse(tbMax.Text).ToString(new CultureInfo("en-US"));
                    jsonObj["Target"] = Double.Parse(tbTarget.Text).ToString(new CultureInfo("en-US"));

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

        private byte[] StringToByteArray(String hex)
        {
            int NumberChars = hex.Length;
            byte[] bytes = new byte[NumberChars / 2];
            for (int i = 0; i < NumberChars; i += 2)
                bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
            return bytes;
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
                tbRazmikProzenja.Text = config.AppSettings.Settings["tbRazmikProzenja"].Value;
                tbAPIUrl.Text = config.AppSettings.Settings["tbAPIUrl"].Value;
                tbStrukturaPodatkov.Text = config.AppSettings.Settings["tbStrukturaPodatkov"].Value;
                tbMin.Text = config.AppSettings.Settings["tbMin"].Value;
                tbMax.Text = config.AppSettings.Settings["tbMax"].Value;
                tbTarget.Text = config.AppSettings.Settings["tbTarget"].Value;
            }
            catch { }
        }
    }
}
