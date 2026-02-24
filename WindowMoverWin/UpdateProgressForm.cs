using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WindowMover
{
    public class UpdateProgressForm : Form
    {
        private Label lblStatus;
        private ProgressBar progressBar;
        private Label lblProgress;
        private Button btnCancel;

        private CancellationTokenSource _cts;
        private readonly UpdateCheckResult _updateInfo;

        public UpdateProgressForm(UpdateCheckResult updateInfo)
        {
            _updateInfo = updateInfo;
            _cts = new CancellationTokenSource();
            InitializeUI();
        }

        private void InitializeUI()
        {
            this.Text = "更新 WindowMover";
            this.Size = new Size(420, 180);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.ShowInTaskbar = true;
            this.BackColor = Color.White;

            Font mainFont = new Font("Microsoft YaHei UI", 9.5F);

            lblStatus = new Label
            {
                Text = $"正在下载 v{_updateInfo.LatestVersion} ...",
                Font = new Font("Microsoft YaHei UI", 10F, FontStyle.Bold),
                ForeColor = Color.FromArgb(50, 50, 50),
                Location = new Point(20, 15),
                Size = new Size(370, 24),
                AutoSize = false
            };

            progressBar = new ProgressBar
            {
                Location = new Point(20, 45),
                Size = new Size(370, 24),
                Style = ProgressBarStyle.Continuous,
                Minimum = 0,
                Maximum = 100,
                Value = 0
            };

            lblProgress = new Label
            {
                Text = "准备下载...",
                Font = mainFont,
                ForeColor = Color.FromArgb(100, 100, 100),
                Location = new Point(20, 75),
                Size = new Size(370, 20),
                AutoSize = false
            };

            btnCancel = new Button
            {
                Text = "取消",
                Font = mainFont,
                Size = new Size(80, 30),
                Location = new Point(310, 100),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(240, 240, 240),
                Cursor = Cursors.Hand
            };
            btnCancel.FlatAppearance.BorderColor = Color.FromArgb(200, 200, 200);
            btnCancel.Click += (s, e) =>
            {
                _cts?.Cancel();
                btnCancel.Enabled = false;
                lblStatus.Text = "正在取消...";
            };

            this.Controls.AddRange(new Control[] { lblStatus, progressBar, lblProgress, btnCancel });

            this.Shown += async (s, e) => await StartDownloadAsync();
            this.FormClosing += (s, e) =>
            {
                _cts?.Cancel();
            };
        }

        private async Task StartDownloadAsync()
        {
            string tempDir = Path.Combine(Path.GetTempPath(), "WindowMover_Update");
            if (!Directory.Exists(tempDir))
            {
                Directory.CreateDirectory(tempDir);
            }

            string zipPath = Path.Combine(tempDir, _updateInfo.AssetName ?? "update.zip");

            try
            {
                var progress = new Progress<(long downloaded, long total)>(p =>
                {
                    if (p.total > 0)
                    {
                        int percent = (int)(p.downloaded * 100 / p.total);
                        progressBar.Value = Math.Min(percent, 100);
                        lblProgress.Text = $"{FormatBytes(p.downloaded)} / {FormatBytes(p.total)}  ({percent}%)";
                    }
                    else
                    {
                        lblProgress.Text = $"已下载 {FormatBytes(p.downloaded)}";
                    }
                });

                await UpdateChecker.DownloadFileAsync(_updateInfo.DownloadUrl, zipPath, progress, _cts.Token);

                // 下载完成
                progressBar.Value = 100;
                lblStatus.Text = "下载完成，正在安装更新...";
                lblProgress.Text = "正在解压并准备替换文件...";
                btnCancel.Enabled = false;

                // 稍等一下让 UI 更新
                await Task.Delay(500);

                // 执行安装
                UpdateChecker.InstallUpdate(zipPath);

                // 退出当前程序
                lblStatus.Text = "更新即将完成，正在重启...";
                await Task.Delay(300);

                // 关闭钩子和托盘图标
                Program.SetHookEnabled(false);
                Environment.Exit(0);
            }
            catch (OperationCanceledException)
            {
                lblStatus.Text = "更新已取消";
                lblProgress.Text = "";
                progressBar.Value = 0;

                // 清理下载文件
                try { if (File.Exists(zipPath)) File.Delete(zipPath); } catch { }

                await Task.Delay(800);
                this.Close();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[WindowMover] Download error: {ex}");
                lblStatus.Text = "下载失败";
                lblProgress.Text = ex.Message;
                progressBar.Value = 0;
                btnCancel.Text = "关闭";
                btnCancel.Enabled = true;
                btnCancel.Click -= null;
                btnCancel.Click += (s, e) => this.Close();
            }
        }

        private static string FormatBytes(long bytes)
        {
            if (bytes < 1024)
                return $"{bytes} B";
            if (bytes < 1024 * 1024)
                return $"{bytes / 1024.0:F1} KB";
            return $"{bytes / (1024.0 * 1024.0):F1} MB";
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _cts?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
