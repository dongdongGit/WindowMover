```markdown
---
name: system-tray
description: Windows system tray (NotifyIcon) and macOS menu bar app implementation.
---

# System Tray & Menu Bar Implementation

## Windows System Tray (MainForm.cs)
### NotifyIcon Setup
```csharp
private NotifyIcon notifyIcon;
private ContextMenuStrip contextMenu;

private void InitializeSystemTray()
{
    notifyIcon = new NotifyIcon
    {
        Icon = new Icon("appicon.ico"),
        Text = "WindowMover - Middle-click title bar to drag",
        Visible = true
    };

    contextMenu = new ContextMenuStrip();
    contextMenu.Items.Add("Settings...", null, (s, e) => ShowSettings());
    contextMenu.Items.Add("Run at Startup", null, (s, e) => ToggleStartup());
    contextMenu.Items.Add("-");
    contextMenu.Items.Add("Exit", null, (s, e) => ExitApplication());

    notifyIcon.ContextMenuStrip = contextMenu;
    notifyIcon.DoubleClick += (s, e) => ShowSettings();
}

private void ShowSettings()
{
    if (settingsForm == null || settingsForm.IsDisposed)
    {
        settingsForm = new SettingsForm();
    }
    settingsForm.Show();
    settingsForm.Activate();
}
```

### Settings Form (MainForm.cs)
```csharp
public partial class SettingsForm : Form
{
    private CheckBox chkEnableDrag;
    private CheckBox chkRunAtStartup;
    private CheckBox chkStartMinimized;

    public SettingsForm()
    {
        InitializeComponent();
        LoadSettings();
    }

    private void InitializeComponent()
    {
        this.Text = "WindowMover Settings";
        this.Size = new Size(300, 200);
        this.FormBorderStyle = FormBorderStyle.FixedDialog;
        this.MaximizeBox = false;

        chkEnableDrag = new CheckBox
        {
            Text = "Enable middle-click drag",
            Checked = Settings.Default.EnableDrag,
            Location = new Point(20, 20)
        };

        chkRunAtStartup = new CheckBox
        {
            Text = "Run at startup",
            Checked = IsInStartupRegistry(),
            Location = new Point(20, 50)
        };

        chkStartMinimized = new CheckBox
        {
            Text = "Start minimized",
            Checked = Settings.Default.StartMinimized,
            Location = new Point(20, 80)
        };

        var okButton = new Button
        {
            Text = "OK",
            DialogResult = DialogResult.OK,
            Location = new Point(180, 130)
        };
        okButton.Click += (s, e) => SaveSettings();

        this.Controls.AddRange(new Control[] { chkEnableDrag, chkRunAtStartup, chkStartMinimized, okButton });
    }

    private void SaveSettings()
    {
        Settings.Default.EnableDrag = chkEnableDrag.Checked;
        Settings.Default.StartMinimized = chkStartMinimized.Checked;
        Settings.Default.Save();
    }
}
```

## macOS Menu Bar (AppDelegate.swift)
### Status Item Setup
```swift
class AppDelegate: NSObject, NSApplicationDelegate {
    var statusItem: NSStatusItem!
    var popover: NSPopover!
    var eventTap: CGMutableTap!

    func applicationDidFinishLaunching(_ notification: Notification) {
        setupStatusItem()
        setupPopover()
        checkAccessibilityPermission()
        setupEventTap()
    }

    func setupStatusItem() {
        statusItem = NSStatusBar.system.statusItem(withLength: NSStatusItem.variableLength)
        statusItem.button?.image = NSImage(named: "AppIcon")
        statusItem.button?.target = self
        statusItem.button?.action = #selector(togglePopover)
    }

    func setupPopover() {
        popover = NSPopover()
        popover.contentViewController = SettingsViewController()
        popover.behavior = .transient
    }

    @objc func togglePopover() {
        if let button = statusItem.button {
            if popover.isShown {
                popover.perform(#selector(popover.close), with: nil, afterDelay: 0)
            } else {
                popover.show(relativeTo: button.bounds, of: button, preferredEdge: .minY)
            }
        }
    }
}
```

### Settings Popover (SettingsViewController)
```swift
class SettingsViewController: NSViewController {
    @IBOutlet weak var enableDragCheckbox: NSButton!
    @IBOutlet weak var launchAtLoginCheckbox: NSButton!
    @IBOutlet weak var permissionStatusLabel: NSTextField!

    override func viewDidLoad() {
        super.viewDidLoad()
        loadSettings()
    }

    func loadSettings() {
        enableDragCheckbox.state = UserDefaults.standard.bool(forKey: "enableDrag") ? .on : .off
        launchAtLoginCheckbox.state = isLaunchAtLoginEnabled() ? .on : .off
        permissionStatusLabel.stringValue = isAccessibilityPermissionGranted() ? "Granted" : "Not granted"
    }

    @IBAction func enableDragChanged(_ sender: NSButton) {
        UserDefaults.standard.set(sender.state == .on, forKey: "enableDrag")
    }

    @IBAction func launchAtLoginChanged(_ sender: NSButton) {
        setLaunchAtLogin(sender.state == .on)
    }
}
```

## Startup Registry (Windows)
```csharp
private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";

public static bool IsInStartupRegistry()
{
    using (var key = Registry.CurrentUser.OpenSubKey(RunKey))
    {
        return key?.GetValue("WindowMover") != null;
    }
}

public static void SetStartupRegistry(bool enable)
{
    using (var key = Registry.CurrentUser.OpenSubKey(RunKey, true))
    {
        if (enable)
        {
            string executablePath = Process.GetCurrentProcess().MainModule?.FileName;
            key?.SetValue("WindowMover", executablePath);
        }
        else
        {
            key?.DeleteValue("WindowMover", false);
        }
    }
}
```
