using System;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using Microsoft.Win32;

namespace WindowMover
{
    class MainForm : Form
    {
        private CheckBox chkDisableHook;     
        private CheckBox chkAutoStart;       
        private CheckBox chkStartMinimized;
        private CheckBox chkRunAsAdmin;
        private CheckBox chkAutoUpdate;
        private NotifyIcon notifyIcon;
        private ContextMenuStrip contextMenu;
        private Icon appIcon;

        private bool _allowShow = true;
        private bool _isFirstLoad = true;
        
        // 关键修复：添加一个标志位，防止加载配置时误触发保存
        private bool _isLoadingSettings = false;

        private readonly Color PrimaryColor = Color.FromArgb(0, 120, 215); 
        private readonly Color BackgroundColor = Color.White;
        private readonly Color TextColor = Color.FromArgb(50, 50, 50);
        private readonly Font MainFont = new Font("Microsoft YaHei UI", 10F, FontStyle.Regular);
        private readonly Font TitleFont = new Font("Microsoft YaHei UI", 14F, FontStyle.Bold);

        public MainForm(Icon icon)
        {
            this.appIcon = icon;
            InitializeComponents();
            LoadSettings(); // 加载设置
            
            // 启动时自动检查更新
            if (chkAutoUpdate.Checked)
            {
                CheckForUpdateAsync(silent: true);
            }
        }

        private void InitializeComponents()
        {
            this.Text = "Window Mover";
            this.Size = new Size(400, 360); 
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.BackColor = BackgroundColor;
            this.Icon = appIcon; 

            Panel headerPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 60,
                BackColor = PrimaryColor,
                Padding = new Padding(20, 0, 0, 0)
            };
            Label lblTitle = new Label
            {
                Text = "Window Mover",
                ForeColor = Color.White,
                Font = TitleFont,
                AutoSize = true,
                Top = 15,
                Left = 20
            };
            headerPanel.Controls.Add(lblTitle);
            this.Controls.Add(headerPanel);

            Label lblHint = new Label
            {
                Text = "提示：程序需在后台运行。\n在任意窗口标题栏点击鼠标中键即可移动。",
                Dock = DockStyle.Bottom,
                Height = 60,
                ForeColor = Color.Gray,
                Font = new Font("Microsoft YaHei UI", 8F),
                TextAlign = ContentAlignment.MiddleCenter,
                Padding = new Padding(10)
            };
            this.Controls.Add(lblHint);

            FlowLayoutPanel contentPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                Padding = new Padding(30, 15, 0, 0),
                AutoScroll = false
            };

            // 1. 禁用功能
            chkDisableHook = new CheckBox
            {
                Text = "禁用鼠标中键移动功能",
                Height = 30,
                Width = 320,
                Font = MainFont,
                ForeColor = TextColor,
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 0, 0, 5)
            };
            chkDisableHook.CheckedChanged += (s, e) => { 
                // 只有在非加载状态下才保存
                if (!_isLoadingSettings)
                {
                    Program.SetHookEnabled(!chkDisableHook.Checked); 
                    SaveSettings(); 
                }
            };
            
            // 2. 开机自启
            chkAutoStart = new CheckBox
            {
                Text = "开机自动启动",
                Height = 30,
                Width = 320,
                Font = MainFont,
                ForeColor = TextColor,
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 0, 0, 5)
            };
            chkAutoStart.CheckedChanged += (s, e) => {
                if (!_isLoadingSettings)
                {
                    SetAutoStart(chkAutoStart.Checked);
                    SaveSettings();
                }
            };

            // 3. 总是最小化启动
            chkStartMinimized = new CheckBox
            {
                Text = "总是最小化启动(隐藏至系统托盘)",
                Height = 30,
                Width = 320,
                Font = MainFont,
                ForeColor = TextColor,
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 0, 0, 5)
            };
            chkStartMinimized.CheckedChanged += (s, e) => {
                if (!_isLoadingSettings)
                {
                    SaveSettings();
                }
            };

            // 4. 以管理员身份启动
            chkRunAsAdmin = new CheckBox
            {
                Text = "以管理员身份启动",
                Height = 30,
                Width = 320,
                Font = MainFont,
                ForeColor = TextColor,
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 0, 0, 5)
            };
            chkRunAsAdmin.CheckedChanged += (s, e) => {
                if (!_isLoadingSettings)
                {
                    SaveSettings();
                    HandleRunAsAdminChanged(chkRunAsAdmin.Checked);
                }
            };

            // 5. 启动时自动检查更新
            chkAutoUpdate = new CheckBox
            {
                Text = "启动时自动检查更新",
                Height = 30,
                Width = 320,
                Font = MainFont,
                ForeColor = TextColor,
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 0, 0, 5)
            };
            chkAutoUpdate.CheckedChanged += (s, e) => {
                if (!_isLoadingSettings)
                {
                    SaveSettings();
                }
            };

            contentPanel.Controls.Add(chkDisableHook);      
            contentPanel.Controls.Add(chkAutoStart);        
            contentPanel.Controls.Add(chkStartMinimized);
            contentPanel.Controls.Add(chkRunAsAdmin);
            contentPanel.Controls.Add(chkAutoUpdate);
            
            this.Controls.Add(contentPanel);
            contentPanel.BringToFront(); 

            InitializeNotifyIcon();
            this.FormClosing += MainForm_FormClosing;
        }

        protected override void SetVisibleCore(bool value)
        {
            if (_isFirstLoad)
            {
                if (chkStartMinimized.Checked)
                {
                    _isFirstLoad = false;
                    _allowShow = false;
                    value = false; 
                    if (!this.IsHandleCreated) CreateHandle();
                }
                else
                {
                    _isFirstLoad = false;
                }
            }
            if (!_allowShow) value = false;
            base.SetVisibleCore(value);
        }

        private void ShowWindow()
        {
            _allowShow = true;
            this.Show();
            this.WindowState = FormWindowState.Normal;
            this.Activate();
        }

        private void InitializeNotifyIcon()
        {
            notifyIcon = new NotifyIcon
            {
                Icon = appIcon,
                Text = "Window Mover",
                Visible = true
            };
            notifyIcon.DoubleClick += (s, e) => ShowWindow();

            contextMenu = new ContextMenuStrip();
            contextMenu.Renderer = new ModernToolStripRenderer(PrimaryColor, TextColor);
            
            contextMenu.Items.Add("显示主界面", null, (s, e) => ShowWindow());
            contextMenu.Items.Add("检查更新", null, (s, e) => CheckForUpdateAsync(silent: false));
            contextMenu.Items.Add(new ToolStripSeparator());
            contextMenu.Items.Add("退出程序", null, (s, e) => ExitApplication());
            notifyIcon.ContextMenuStrip = contextMenu;
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                _allowShow = false;
                this.Hide();
            }
        }

        private void ExitApplication()
        {
            notifyIcon.Visible = false;
            Program.SetHookEnabled(false);
            Application.Exit();
        }

        private void LoadSettings()
        {
            // 关键修复：开始加载时，锁住保存功能
            _isLoadingSettings = true;
            try
            {
                using (RegistryKey key = Registry.CurrentUser.CreateSubKey(@"Software\WindowMover"))
                {
                    if (key != null)
                    {
                        chkDisableHook.Checked = Convert.ToBoolean(key.GetValue("DisableHook", false)); 
                        chkAutoStart.Checked = Convert.ToBoolean(key.GetValue("AutoStart", false));
                        chkStartMinimized.Checked = Convert.ToBoolean(key.GetValue("StartMinimized", false));
                        chkRunAsAdmin.Checked = Convert.ToBoolean(key.GetValue("RunAsAdmin", false));
                        chkAutoUpdate.Checked = Convert.ToBoolean(key.GetValue("AutoCheckUpdate", true));
                        
                        // 应用钩子状态
                        Program.SetHookEnabled(!chkDisableHook.Checked);
                    }
                }
            }
            finally
            {
                // 关键修复：加载完成后，解锁保存功能
                _isLoadingSettings = false;
            }
        }

        private void SaveSettings()
        {
            // 如果正在加载，坚决不保存，防止覆盖
            if (_isLoadingSettings) return;

            using (RegistryKey key = Registry.CurrentUser.CreateSubKey(@"Software\WindowMover"))
            {
                if (key != null)
                {
                    key.SetValue("DisableHook", chkDisableHook.Checked);
                    key.SetValue("AutoStart", chkAutoStart.Checked);
                    key.SetValue("StartMinimized", chkStartMinimized.Checked);
                    key.SetValue("RunAsAdmin", chkRunAsAdmin.Checked);
                    key.SetValue("AutoCheckUpdate", chkAutoUpdate.Checked);
                }
            }
        }

        private void SetAutoStart(bool enable)
        {
            try
            {
                string taskName = "WindowMover_AutoStart";
                string exePath = Application.ExecutablePath;
                bool useAdmin = chkRunAsAdmin.Checked;
                string runLevel = useAdmin ? "HIGHEST" : "LIMITED";

                Debug.WriteLine($"[WindowMover] SetAutoStart: enable={enable}, runLevel={runLevel}, exePath={exePath}");

                if (enable)
                {
                    // 使用 schtasks 创建登录触发的计划任务
                    string args = $"/Create /TN \"{taskName}\" /TR \"\\\"{exePath}\\\"\" /SC ONLOGON /RL {runLevel} /F";
                    int exitCode = RunSchtasks(args);

                    if (exitCode != 0)
                    {
                        throw new Exception($"schtasks 创建任务失败，退出码: {exitCode}");
                    }
                    Debug.WriteLine("[WindowMover] SetAutoStart: 计划任务创建成功");
                }
                else
                {
                    // 删除计划任务
                    string args = $"/Delete /TN \"{taskName}\" /F";
                    RunSchtasks(args);
                    Debug.WriteLine("[WindowMover] SetAutoStart: 计划任务已删除");
                }

                // 清理旧的注册表启动项
                CleanupRegistryAutoStart();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[WindowMover] SetAutoStart 异常: {ex.Message}");
                MessageBox.Show($"设置开机自启动失败：{ex.Message}",
                    "设置失败", MessageBoxButtons.OK, MessageBoxIcon.Warning);

                if (!_isLoadingSettings) chkAutoStart.Checked = !enable;
            }
        }

        private int RunSchtasks(string arguments)
        {
            var psi = new ProcessStartInfo("schtasks.exe", arguments)
            {
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };
            using (var process = Process.Start(psi))
            {
                process.WaitForExit(10000);
                string stdout = process.StandardOutput.ReadToEnd();
                string stderr = process.StandardError.ReadToEnd();
                Debug.WriteLine($"[WindowMover] schtasks stdout: {stdout}");
                Debug.WriteLine($"[WindowMover] schtasks stderr: {stderr}");
                return process.ExitCode;
            }
        }

        private void CleanupRegistryAutoStart()
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", true))
                {
                    if (key != null)
                    {
                        key.DeleteValue("WindowMover", false);
                    }
                }
                using (RegistryKey key = Registry.LocalMachine.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", true))
                {
                    if (key != null)
                    {
                        key.DeleteValue("WindowMover", false);
                    }
                }
            }
            catch { }
        }

        private void HandleRunAsAdminChanged(bool enableAdmin)
        {
            if (enableAdmin && !Program.IsRunAsAdmin())
            {
                // 需要提升权限：以管理员身份重启
                try
                {
                    var psi = new ProcessStartInfo(Application.ExecutablePath)
                    {
                        UseShellExecute = true,
                        Verb = "runas"
                    };
                    Process.Start(psi);
                    // 退出当前实例
                    notifyIcon.Visible = false;
                    Program.SetHookEnabled(false);
                    Environment.Exit(0);
                }
                catch (System.ComponentModel.Win32Exception)
                {
                    // 用户取消了 UAC 提示
                    _isLoadingSettings = true;
                    chkRunAsAdmin.Checked = false;
                    _isLoadingSettings = false;
                    SaveSettings();
                }
            }
            else if (!enableAdmin && Program.IsRunAsAdmin())
            {
                // 取消管理员模式：以普通权限重启
                try
                {
                    var psi = new ProcessStartInfo(Application.ExecutablePath)
                    {
                        UseShellExecute = true
                    };
                    Process.Start(psi);
                    notifyIcon.Visible = false;
                    Program.SetHookEnabled(false);
                    Environment.Exit(0);
                }
                catch { }
            }

            // 如果已经启用了自启动，重新创建任务以更新权限级别
            if (chkAutoStart.Checked)
            {
                SetAutoStart(true);
            }
        }

        private async void CheckForUpdateAsync(bool silent)
        {
            var result = await UpdateChecker.CheckForUpdateAsync();
            UpdateChecker.ShowUpdateResult(result, silent, this);
        }
    }

    public class ModernToolStripRenderer : ToolStripProfessionalRenderer
    {
        private Color _primaryColor;
        private Color _textColor;

        public ModernToolStripRenderer(Color primary, Color text) : base(new ModernColorTable(primary))
        {
            _primaryColor = primary;
            _textColor = text;
            this.RoundedEdges = false;
        }

        protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
        {
            e.TextColor = e.Item.Selected ? Color.White : _textColor;
            base.OnRenderItemText(e);
        }
    }

    public class ModernColorTable : ProfessionalColorTable
    {
        private Color _primaryColor;
        public ModernColorTable(Color primary) { _primaryColor = primary; }

        public override Color MenuItemSelected => _primaryColor;
        public override Color MenuItemBorder => _primaryColor;
        public override Color MenuItemSelectedGradientBegin => _primaryColor;
        public override Color MenuItemSelectedGradientEnd => _primaryColor;
        public override Color MenuBorder => _primaryColor;
        public override Color ImageMarginGradientBegin => Color.White;
        public override Color ImageMarginGradientMiddle => Color.White;
        public override Color ImageMarginGradientEnd => Color.White;
        public override Color ToolStripDropDownBackground => Color.White;
    }
}