using System;
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
        }

        private void InitializeComponents()
        {
            this.Text = "Window Mover";
            this.Size = new Size(400, 280); 
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

            contentPanel.Controls.Add(chkDisableHook);      
            contentPanel.Controls.Add(chkAutoStart);        
            contentPanel.Controls.Add(chkStartMinimized);   
            
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
                }
            }
        }

        private void SetAutoStart(bool enable)
        {
            try {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", true))
                {
                    if (key != null)
                    {
                        if (enable) key.SetValue("WindowMover", Application.ExecutablePath);
                        else key.DeleteValue("WindowMover", false);
                    }
                }
            } catch { 
                // 如果设置失败，不要触发 SaveSettings 的连锁反应，但因为有 _isLoadingSettings 保护，这里相对安全
                if (!_isLoadingSettings) chkAutoStart.Checked = !enable; 
            }
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