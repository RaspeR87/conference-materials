using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Text;
using System.Threading.Tasks;

namespace GKPCStuff
{
    public class MemoryLoad
    {
        public double Load { get; set; }
        public static List<MemoryLoad> Loads
        {
            get
            {
                List<MemoryLoad> result = new List<MemoryLoad>();
                try
                {
                    ManagementObjectSearcher searcher = new ManagementObjectSearcher(@"root\CIMV2", "SELECT * FROM Win32_OperatingSystem");
                    foreach (ManagementObject obj in searcher.Get())
                    {
                        double free = Double.Parse(obj["FreePhysicalMemory"].ToString());
                        double total = Double.Parse(obj["TotalVisibleMemorySize"].ToString());
                        result.Add(new MemoryLoad { Load = Math.Round(((total - free) / total * 100), 2) });
                    }
                }
                catch { }
                return result;

            }
        }
    }
}
