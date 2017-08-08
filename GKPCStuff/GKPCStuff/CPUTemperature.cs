using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Text;
using System.Threading.Tasks;

namespace GKPCStuff
{
    public class CPUTemperature
    {
        public double CurrentValue { get; set; }
        public static List<CPUTemperature> Temperatures
        {
            get
            {
                List<CPUTemperature> result = new List<CPUTemperature>();
                try { 
                    ManagementObjectSearcher searcher = new ManagementObjectSearcher(@"root\WMI", "SELECT * FROM MSAcpi_ThermalZoneTemperature");
                    foreach (ManagementObject obj in searcher.Get())
                    {
                        Double temp = Convert.ToDouble(obj["CurrentTemperature"].ToString());
                        temp = (temp - 2732) / 10.0;
                        result.Add(new CPUTemperature { CurrentValue = temp });
                    }
                }
                catch { }
                return result;

            }
        }
    }
}
