using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WindowMover
{
    public class UpdateCheckResult
    {
        public bool HasUpdate { get; set; }
        public string LatestVersion { get; set; }
        public string CurrentVersion { get; set; }
        public string ReleaseUrl { get; set; }
        public string DownloadUrl { get; set; }
        public string AssetName { get; set; }
        public long AssetSize { get; set; }
        public string ErrorMessage { get; set; }
    }

    public static class UpdateChecker
    {
        private const string GitHubApiUrl = "https://api.github.com/repos/dongdongGit/WindowMover/releases/latest";
        private const string ReleasesPageUrl = "https://github.com/dongdongGit/WindowMover/releases/latest";

        private static readonly HttpClient httpClient = new HttpClient();

        static UpdateChecker()
        {
            httpClient.DefaultRequestHeaders.Add("User-Agent", "WindowMover-UpdateChecker");
            httpClient.Timeout = TimeSpan.FromSeconds(15);
        }

        public static string GetCurrentVersion()
        {
            var version = Assembly.GetExecutingAssembly().GetName().Version;
            if (version != null)
            {
                return $"{version.Major}.{version.Minor}.{version.Build}";
            }
            return "1.0.0";
        }

        /// <summary>
        /// 根据当前运行环境获取匹配的资源名称关键字
        /// </summary>
        private static string GetArchitectureKeyword()
        {
            var arch = RuntimeInformation.ProcessArchitecture;
            switch (arch)
            {
                case Architecture.X64: return "win-x64";
                case Architecture.X86: return "win-x86";
                case Architecture.Arm64: return "win-arm64";
                default: return "win-x64";
            }
        }

        /// <summary>
        /// 判断当前是否为独立部署（self-contained）
        /// </summary>
        private static bool IsSelfContained()
        {
            // 如果目录下有 coreclr.dll，说明是 self-contained 部署
            string dir = AppContext.BaseDirectory;
            return File.Exists(Path.Combine(dir, "coreclr.dll"));
        }

        /// <summary>
        /// 从 assets 列表中找到匹配当前平台的下载链接
        /// </summary>
        private static (string downloadUrl, string name, long size) FindMatchingAsset(JsonElement assets)
        {
            string archKey = GetArchitectureKeyword();
            bool selfContained = IsSelfContained();
            string deployType = selfContained ? "standalone" : "net8";

            Debug.WriteLine($"[WindowMover] Looking for asset: arch={archKey}, deploy={deployType}");

            // 优先精确匹配 arch + deploy type
            foreach (var asset in assets.EnumerateArray())
            {
                string name = asset.GetProperty("name").GetString() ?? "";
                if (name.Contains(archKey, StringComparison.OrdinalIgnoreCase) &&
                    name.Contains(deployType, StringComparison.OrdinalIgnoreCase) &&
                    name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
                {
                    string url = asset.GetProperty("browser_download_url").GetString();
                    long size = asset.GetProperty("size").GetInt64();
                    Debug.WriteLine($"[WindowMover] Matched asset: {name}");
                    return (url, name, size);
                }
            }

            // 回退：只匹配 arch
            foreach (var asset in assets.EnumerateArray())
            {
                string name = asset.GetProperty("name").GetString() ?? "";
                if (name.Contains(archKey, StringComparison.OrdinalIgnoreCase) &&
                    name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
                {
                    string url = asset.GetProperty("browser_download_url").GetString();
                    long size = asset.GetProperty("size").GetInt64();
                    Debug.WriteLine($"[WindowMover] Fallback matched asset: {name}");
                    return (url, name, size);
                }
            }

            return (null, null, 0);
        }

        public static async Task<UpdateCheckResult> CheckForUpdateAsync()
        {
            var result = new UpdateCheckResult
            {
                CurrentVersion = GetCurrentVersion()
            };

            try
            {
                var response = await httpClient.GetAsync(GitHubApiUrl);
                if (!response.IsSuccessStatusCode)
                {
                    result.ErrorMessage = $"GitHub API 返回 {(int)response.StatusCode}";
                    return result;
                }

                var json = await response.Content.ReadAsStringAsync();
                using (var doc = JsonDocument.Parse(json))
                {
                    var root = doc.RootElement;

                    string tagName = root.GetProperty("tag_name").GetString();
                    string htmlUrl = root.GetProperty("html_url").GetString();

                    // 去掉版本号前缀 "v"
                    string latestVersionStr = tagName.TrimStart('v', 'V');

                    result.LatestVersion = latestVersionStr;
                    result.ReleaseUrl = htmlUrl ?? ReleasesPageUrl;

                    // 比较版本
                    if (Version.TryParse(latestVersionStr, out Version latestVersion) &&
                        Version.TryParse(result.CurrentVersion, out Version currentVersion))
                    {
                        result.HasUpdate = latestVersion > currentVersion;
                    }
                    else
                    {
                        result.HasUpdate = string.Compare(latestVersionStr, result.CurrentVersion, StringComparison.OrdinalIgnoreCase) > 0;
                    }

                    // 查找匹配的下载资源
                    if (result.HasUpdate && root.TryGetProperty("assets", out JsonElement assets))
                    {
                        var (downloadUrl, assetName, assetSize) = FindMatchingAsset(assets);
                        result.DownloadUrl = downloadUrl;
                        result.AssetName = assetName;
                        result.AssetSize = assetSize;
                    }
                }
            }
            catch (TaskCanceledException)
            {
                result.ErrorMessage = "检查更新超时，请检查网络连接";
            }
            catch (HttpRequestException ex)
            {
                result.ErrorMessage = $"网络错误: {ex.Message}";
            }
            catch (Exception ex)
            {
                result.ErrorMessage = $"检查更新失败: {ex.Message}";
                Debug.WriteLine($"[WindowMover] UpdateChecker error: {ex}");
            }

            return result;
        }

        /// <summary>
        /// 下载文件并报告进度
        /// </summary>
        public static async Task DownloadFileAsync(string url, string destPath, IProgress<(long downloaded, long total)> progress, CancellationToken cancellationToken)
        {
            using (var request = new HttpRequestMessage(HttpMethod.Get, url))
            using (var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken))
            {
                response.EnsureSuccessStatusCode();
                long totalBytes = response.Content.Headers.ContentLength ?? -1;

                using (var contentStream = await response.Content.ReadAsStreamAsync())
                using (var fileStream = new FileStream(destPath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true))
                {
                    var buffer = new byte[8192];
                    long totalRead = 0;
                    int bytesRead;

                    while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length, cancellationToken)) > 0)
                    {
                        await fileStream.WriteAsync(buffer, 0, bytesRead, cancellationToken);
                        totalRead += bytesRead;
                        progress?.Report((totalRead, totalBytes));
                    }
                }
            }
        }

        /// <summary>
        /// 执行更新安装：解压 ZIP 并创建批处理脚本替换文件后重启
        /// </summary>
        public static void InstallUpdate(string zipPath)
        {
            string appDir = AppContext.BaseDirectory;
            string appExe = Application.ExecutablePath;
            string tempExtractDir = Path.Combine(Path.GetTempPath(), "WindowMover_Update_Extract");

            // 清理旧的解压目录
            if (Directory.Exists(tempExtractDir))
            {
                Directory.Delete(tempExtractDir, true);
            }

            // 解压
            ZipFile.ExtractToDirectory(zipPath, tempExtractDir);

            // 创建批处理脚本：等待当前进程退出 → 复制文件 → 重启
            string batPath = Path.Combine(Path.GetTempPath(), "WindowMover_Update.bat");
            string batContent = $@"@echo off
chcp 65001 >nul
echo 正在更新 WindowMover，请稍候...
:wait
tasklist /FI ""PID eq {Process.GetCurrentProcess().Id}"" 2>nul | find ""{Process.GetCurrentProcess().Id}"" >nul
if not errorlevel 1 (
    timeout /t 1 /nobreak >nul
    goto wait
)
echo 正在复制文件...
xcopy ""{tempExtractDir}\*"" ""{appDir}"" /E /Y /Q
echo 启动 WindowMover...
start """" ""{appExe}""
rd /s /q ""{tempExtractDir}""
del ""{zipPath}""
del ""%~f0""
";

            File.WriteAllText(batPath, batContent, System.Text.Encoding.UTF8);

            // 启动批处理脚本（隐藏窗口）
            var psi = new ProcessStartInfo(batPath)
            {
                CreateNoWindow = true,
                UseShellExecute = false,
                WindowStyle = ProcessWindowStyle.Hidden
            };
            Process.Start(psi);
        }

        /// <summary>
        /// 显示检查更新结果（静默模式 or 交互模式）
        /// </summary>
        public static void ShowUpdateResult(UpdateCheckResult result, bool silent, Form ownerForm)
        {
            if (result.ErrorMessage != null)
            {
                if (!silent)
                {
                    MessageBox.Show(result.ErrorMessage, "检查更新", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
                return;
            }

            if (result.HasUpdate)
            {
                string msg;
                MessageBoxButtons buttons;

                if (result.DownloadUrl != null)
                {
                    msg = $"发现新版本！\n\n当前版本: v{result.CurrentVersion}\n最新版本: v{result.LatestVersion}\n资源包: {result.AssetName}\n\n点击「是」立即下载并安装更新\n点击「否」跳过本次更新";
                    buttons = MessageBoxButtons.YesNo;
                }
                else
                {
                    msg = $"发现新版本！\n\n当前版本: v{result.CurrentVersion}\n最新版本: v{result.LatestVersion}\n\n未找到匹配的安装包，是否前往下载页面？";
                    buttons = MessageBoxButtons.YesNo;
                }

                var dialogResult = MessageBox.Show(msg, "发现新版本", buttons, MessageBoxIcon.Information);

                if (dialogResult == DialogResult.Yes)
                {
                    if (result.DownloadUrl != null)
                    {
                        // 启动下载进度窗口
                        var progressForm = new UpdateProgressForm(result);
                        progressForm.ShowDialog(ownerForm);
                    }
                    else
                    {
                        // 回退：打开浏览器
                        try
                        {
                            Process.Start(new ProcessStartInfo(result.ReleaseUrl)
                            {
                                UseShellExecute = true
                            });
                        }
                        catch { }
                    }
                }
            }
            else if (!silent)
            {
                MessageBox.Show(
                    $"当前已是最新版本 (v{result.CurrentVersion})",
                    "检查更新",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
        }
    }
}
