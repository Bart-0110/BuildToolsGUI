using System;
using System.Management;

namespace UninstallJava
{
    public class Program
    {
        public static void Main(string[] args)
        {
            try
            {
                ManagementObjectSearcher mos = new ManagementObjectSearcher("SELECT * FROM Win32_Product");
                foreach (ManagementObject mo in mos.Get())
                {
                    if (mo["Name"].ToString().ToLower().StartsWith("java "))
                    {
                        try
                        {
                            Environment.Exit(Convert.ToInt32(mo.InvokeMethod("Uninstall", null)));
                        }
                        catch (Exception)
                        {
                            Environment.Exit(1);
                        }
                    }
                }
            }
            catch (Exception)
            {
                Environment.Exit(1);
            }
            Environment.Exit(1);
        }
    }
}
