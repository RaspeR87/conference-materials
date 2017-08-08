using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Text;
using System.Threading.Tasks;

namespace GKPCStuff
{
    public class CPULoad
    {
        public double PercentProcessorTime { get; set; }
        public static List<CPULoad> Loads
        {
            get
            {
                List<CPULoad> result = new List<CPULoad>();
                try
                {
                    ManagementObjectSearcher searcher = new ManagementObjectSearcher(@"root\CIMV2", "SELECT * FROM Win32_PerfFormattedData_Counters_ProcessorInformation");
                    foreach (ManagementObject obj in searcher.Get())
                    {
                        result.Add(new CPULoad {PercentProcessorTime = Convert.ToDouble(obj["PercentProcessorTime"].ToString()) });
                    }
                }
                catch { }
                return result;

            }
        }
    }
}
