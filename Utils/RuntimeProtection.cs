using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace octonev2.Utils
{
    public static class DecompilerDetection
    {
        private static readonly string[] blacklistedProcesses = new[]
        {
            "dnspy", "ilspy", "reflector", "dotpeek", "de4dot", "fiddler",
            "wireshark", "ida", "ollydbg", "x32dbg", "x64dbg", "cheatengine"
        };

        public static void StartDetectionLoop()
        {
            new Thread(() =>
            {
                while (true)
                {
                    if (IsDecompilerRunning())
                    {
                        Environment.Exit(0);
                    }
                    Thread.Sleep(1000);
                }
            })
            { IsBackground = true }.Start();
        }

        private static bool IsDecompilerRunning()
        {
            var processes = Process.GetProcesses();
            return processes.Any(p => blacklistedProcesses.Contains(p.ProcessName.ToLower()));
        }
    }
}
