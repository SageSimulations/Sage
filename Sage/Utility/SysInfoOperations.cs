/* This source code licensed under the GNU Affero General Public License */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;

namespace Highpoint.Sage.Utility {
    /// <summary>
    /// Wrapper for some key System Info functions.
    /// </summary>
    public static class SysInfoOperations {
        /// <summary>
        /// Gets the CPU IDs for the processors.
        /// </summary>
        /// <returns></returns>
        public static List<string> GetCpuid() {

            ManagementClass mc = new ManagementClass("Win32_Processor");
            ManagementObjectCollection moc = mc.GetInstances();

            return (from ManagementObject mo in moc select mo.Properties["ProcessorId"].Value.ToString()).ToList();
        }

        /// <summary>
        /// return Volume Serial Number from hard drive
        /// </summary>
        /// <param name="strDriveLetter">[optional] Drive letter</param>
        /// <returns>[string] VolumeSerialNumber</returns>
        public static string GetVolumeSerial(string strDriveLetter) {
            if (string.IsNullOrEmpty(strDriveLetter))
                strDriveLetter = "C";
            ManagementObject disk =
                new ManagementObject("win32_logicaldisk.deviceid=\"" + strDriveLetter + ":\"");
            disk.Get();
            return disk["VolumeSerialNumber"].ToString();
        }

        /// <summary>
        /// Returns MAC Address from first Network Card in Computer
        /// </summary>
        /// <returns>[string] MAC Address</returns>
        public static string GetMacAddress() {
            ManagementClass mc = new ManagementClass("Win32_NetworkAdapterConfiguration");
            ManagementObjectCollection moc = mc.GetInstances();
            string macAddress = string.Empty;
            foreach (var o in moc) {
                var mo = (ManagementObject) o;
                if (macAddress == string.Empty)  // only return MAC Address from first card
                {
                    if ((bool)mo["IPEnabled"])
                        macAddress = mo["MacAddress"].ToString();
                }
                mo.Dispose();
            }
            macAddress = macAddress.Replace(":", "");
            return macAddress;
        }

        /// <summary>
        /// Return processorId from first CPU in machine
        /// </summary>
        /// <returns>[string] ProcessorId</returns>
        public static string GetCpuId() {
            string cpuInfo = string.Empty;
            ManagementClass mc = new ManagementClass("Win32_Processor");
            ManagementObjectCollection moc = mc.GetInstances();
            foreach (ManagementBaseObject mo in moc)
            {
                if (String.Empty.Equals(cpuInfo) ) {// only return cpuInfo from first CPU
                    cpuInfo = mo.Properties["ProcessorId"].Value.ToString();
                }
            }
            return cpuInfo;
        }

        /// <summary>
        /// Gets the CPU load percentages.
        /// </summary>
        /// <returns></returns>
        public static List<string> GetCpuLoads() {

            ManagementClass mc = new ManagementClass("Win32_Processor");
            ManagementObjectCollection moc = mc.GetInstances();

            return (from ManagementObject mo in moc select mo.Properties["LoadPercentage"].Value + "%").ToList();
        }

        /// <summary>
        /// Gets the CPU level 2 caches in KB.
        /// </summary>
        /// <returns></returns>
        public static List<string> GetCpul2CachesInKb() {

            ManagementClass mc = new ManagementClass("Win32_Processor");
            ManagementObjectCollection moc = mc.GetInstances();

            return (from ManagementObject mo in moc select mo.Properties["L2CacheSize"].Value + "KB").ToList();
        }
    }
}
