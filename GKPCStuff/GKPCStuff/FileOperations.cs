using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Text;
using System.Threading.Tasks;

namespace GKPCStuff
{
    public class FileOperations
    {
        public double FileReadOperationsPerSec { get; set; }
        public double FileWriteOperationsPerSec { get; set; }
        public double Processes { get; set; }
        public double Threads { get; set; }
        public Int64 SystemUpTime { get; set; }
        public static List<FileOperations> Datas
        {
            get
            {
                List<FileOperations> result = new List<FileOperations>();
                try
                {
                    ManagementObjectSearcher searcher = new ManagementObjectSearcher(@"root\CIMV2", "SELECT * FROM Win32_PerfRawData_PerfOS_System");
                    foreach (ManagementObject obj in searcher.Get())
                    {
                        result.Add(new FileOperations
                        {
                            FileReadOperationsPerSec = Double.Parse(obj["FileReadOperationsPerSec"].ToString()),
                            FileWriteOperationsPerSec = Double.Parse(obj["FileWriteOperationsPerSec"].ToString()),
                            Processes = Double.Parse(obj["Processes"].ToString()),
                            Threads = Double.Parse(obj["Threads"].ToString()),
                            SystemUpTime = Int64.Parse(obj["SystemUpTime"].ToString())
                        });
                    }
                }
                catch { }
                return result;

            }
        }
    }
}
