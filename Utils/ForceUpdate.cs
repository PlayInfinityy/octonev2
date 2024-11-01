using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace octonev2.Utils
{
    public class ForceUpdate
    {
        private const string VERSION_URL = "YOUR_VERSION_CHECK_URL";
        private const string UPDATE_URL = "YOUR_UPDATE_FILE_URL";
        private readonly string currentVersion;

        public ForceUpdate()
        {
            currentVersion = Assembly.GetExecutingAssembly().GetName().Version.ToString();
        }

        public async Task CheckAndUpdate()
        {
            try
            {
                using var client = new HttpClient();
                var latestVersion = await client.GetStringAsync(VERSION_URL);

                if (latestVersion.Trim() != currentVersion)
                {
                    var result = MessageBox.Show(
                        $"New version {latestVersion} available. Current version: {currentVersion}\nWould you like to update now?",
                        "Update Available",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Information);

                    if (result == DialogResult.Yes)
                    {
                        await PerformUpdate();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Update check failed: {ex.Message}", "Update Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async Task PerformUpdate()
        {
            using var client = new HttpClient();
            var updateData = await client.GetByteArrayAsync(UPDATE_URL);
            var tempPath = Path.GetTempFileName();
            File.WriteAllBytes(tempPath, updateData);

            var updaterPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Updater.exe");
            Process.Start(updaterPath, $"\"{tempPath}\" \"{Application.ExecutablePath}\"");
            Application.Exit();
        }

        private string GetPublishedExePath()
        {
            return Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "bin", "Release", "net8.0-windows", "win-x64", "publish",
                "octonev2.exe"
            );
        }

    }
}
