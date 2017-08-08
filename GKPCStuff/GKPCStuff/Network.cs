using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Text;
using System.Threading.Tasks;

namespace GKPCStuff
{
    public class Network
    {
        public double BytesReceivedPersec { get; set; }
        public double BytesSentPersec { get; set; }
        public double BytesTotalPersec { get; set; }
        public double CurrentBandwidth { get; set; }
        public static List<Network> Datas
        {
            get
            {
                List<Network> result = new List<Network>();
                try
                {
                    ManagementObjectSearcher searcher = new ManagementObjectSearcher(@"root\CIMV2", "SELECT * FROM Win32_PerfRawData_Tcpip_NetworkInterface");
                    foreach (ManagementObject obj in searcher.Get())
                    {
                        result.Add(new Network {
                            BytesReceivedPersec = Double.Parse(obj["BytesReceivedPersec"].ToString()),
                            BytesSentPersec = Double.Parse(obj["BytesSentPersec"].ToString()),
                            BytesTotalPersec = Double.Parse(obj["BytesTotalPersec"].ToString()),
                            CurrentBandwidth = Double.Parse(obj["CurrentBandwidth"].ToString())
                        });
                    }
                }
                catch { }
                return result;

            }
        }
    }
}
