using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Text;
using System.Threading.Tasks;

namespace GKPCStuff
{
    public class DiscTemperature
    {
        public double CurrentValue { get; set; }
        public static List<DiscTemperature> Temperatures
        {
            get
            {
                List<DiscTemperature> result = new List<DiscTemperature>();
                try
                {
                    ManagementObjectSearcher searcher = new ManagementObjectSearcher(@"root\WMI", "SELECT * FROM MSStorageDriver_ATAPISmartData");
                    foreach (ManagementObject obj in searcher.Get())
                    {
                        byte[] temp = (byte[])(obj["VendorSpecific"]);
                        result.Add(new DiscTemperature { CurrentValue = temp[163] });
                    }
                }
                catch { }
                return result;

            }
        }
    }
}
