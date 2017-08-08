using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Text;
using System.Threading.Tasks;

namespace GKPCStuff
{
    class DiscLoad
    {
        public double Load { get; set; }
        public double UsedSpace { get; set; }
        public static List<DiscLoad> Loads
        {
            get
            {
                List<DiscLoad> result = new List<DiscLoad>();
                try
                {
                    ManagementObjectSearcher searcher = new ManagementObjectSearcher(@"root\CIMV2", "SELECT * FROM Win32_LogicalDisk");
                    foreach (ManagementObject obj in searcher.Get())
                    {
                        double free = Double.Parse(obj["FreeSpace"].ToString());
                        double total = Double.Parse(obj["Size"].ToString());
                        result.Add(new DiscLoad
                        {
                            Load = Math.Round(((total - free) / total * 100), 2),
                            UsedSpace = Math.Round((total - free) / 1024 / 1024 / 1024, 2)
                        });
                    }
                }
                catch { }
                return result;

            }
        }
    }
}
