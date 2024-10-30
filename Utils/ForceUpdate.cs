using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text.Json;




public class GitHubRelease
{
    public string tag_name { get; set; }
    public List<GitHubAsset> assets { get; set; }
}

public class GitHubAsset
{
    public string browser_download_url { get; set; }
}




public class ForceUpdate
{
    private const string GITHUB_API_URL = "https://api.github.com/repos/YOUR_USERNAME/YOUR_REPO/releases/latest";

    public async Task CheckAndUpdate()
    {
        using var client = new HttpClient();
        client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("OctoneLoader", "1.0"));

        var response = await client.GetStringAsync(GITHUB_API_URL);
        var release = JsonSerializer.Deserialize<GitHubRelease>(response);

        if (release.assets.Count > 0)
        {
            string downloadUrl = release.assets[0].browser_download_url;
            string updatePath = Path.Combine(Path.GetTempPath(), "OctoneUpdate.exe");

            await DownloadAndReplace(downloadUrl, updatePath);
        }
    }

    private async Task DownloadAndReplace(string downloadUrl, string updatePath)
    {
        using var client = new HttpClient();
        var bytes = await client.GetByteArrayAsync(downloadUrl);
        await File.WriteAllBytesAsync(updatePath, bytes);

        string batchPath = Path.Combine(Path.GetTempPath(), "update.bat");
        string currentExe = Application.ExecutablePath;

        // Create batch file to replace current exe
        var batchCommands = new[]
        {
            "@echo off",
            "timeout /t 1 /nobreak > NUL",
            $"copy /Y \"{updatePath}\" \"{currentExe}\"",
            $"start \"\" \"{currentExe}\"",
            "del \"%~f0\"",
            "exit"
        };

        await File.WriteAllLinesAsync(batchPath, batchCommands);
        Process.Start(batchPath);
        Application.Exit();
    }
}
