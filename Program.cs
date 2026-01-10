using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Threading;
using System.Windows.Forms;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Automation;

// 显式声明支持 Windows 7+，消除跨平台警告
[assembly: System.Runtime.Versioning.SupportedOSPlatform("windows7.0")]

namespace WindowMover
{
    class Program
    {
        #region Core Hook Logic & P/Invoke
        private const int WH_MOUSE_LL = 14;
        private const int WM_MBUTTONDOWN = 0x0207;
        private const int WM_NCHITTEST = 0x0084;
        private const int HTCAPTION = 2; 
        
        // 窗口位置标志
        private const uint SWP_NOSIZE = 0x0001;
        private const uint SWP_NOMOVE = 0x0002;
        private const uint SWP_SHOWWINDOW = 0x0040;
        private const uint SWP_NOCOPYBITS = 0x0100;
        private const uint SWP_FRAMECHANGED = 0x0020;

        // Z-Order 常量
        private static readonly IntPtr HWND_TOP = IntPtr.Zero;

        private static IntPtr hookId = IntPtr.Zero;
        private static LowLevelMouseProc hookProc;
        private static bool isHookEnabled = true;

        [DllImport("user32.dll")] private static extern bool SetProcessDpiAwarenessContext(IntPtr value);
        private static readonly IntPtr DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2 = (IntPtr)(-4);
        
        [DllImport("user32.dll")] private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelMouseProc lpfn, IntPtr hMod, uint dwThreadId);
        [DllImport("user32.dll")] private static extern bool UnhookWindowsHookEx(IntPtr hhk);
        [DllImport("user32.dll")] private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);
        [DllImport("user32.dll")] private static extern IntPtr WindowFromPoint(Point point);
        [DllImport("user32.dll")] private static extern IntPtr GetAncestor(IntPtr hwnd, uint gaFlags);
        private const uint GA_ROOT = 2;
        [DllImport("user32.dll")] private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);
        [DllImport("user32.dll")] private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);
        [DllImport("user32.dll")] private static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint dwFlags);
        [DllImport("user32.dll")] private static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFO lpmi);
        [DllImport("user32.dll")] private static extern bool EnumDisplayMonitors(IntPtr hdc, IntPtr lprcClip, EnumMonitorsDelegate lpfnEnum, IntPtr dwData);
        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern IntPtr SendMessageTimeout(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam, uint fuFlags, uint uTimeout, out IntPtr lpdwResult);
        private const uint SMTO_ABORTIFHUNG = 0x0002;
        [DllImport("kernel32.dll")] private static extern IntPtr GetModuleHandle(string lpModuleName);

        [DllImport("user32.dll", SetLastError = true)] private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        // 强力置顶相关
        [DllImport("user32.dll")] private static extern bool SetForegroundWindow(IntPtr hWnd);
        [DllImport("user32.dll")] private static extern IntPtr GetForegroundWindow();
        [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(IntPtr hWnd, IntPtr ProcessId);
        [DllImport("user32.dll")] private static extern bool AttachThreadInput(uint idAttach, uint idAttachTo, bool fAttach);
        [DllImport("user32.dll")] private static extern bool BringWindowToTop(IntPtr hWnd);
        [DllImport("user32.dll")] private static extern bool IsIconic(IntPtr hWnd);
        [DllImport("kernel32.dll")] private static extern uint GetCurrentThreadId();
        [DllImport("user32.dll")] private static extern bool IsZoomed(IntPtr hWnd);
        [DllImport("user32.dll")] private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
        private const int SW_RESTORE = 9;
        private const int SW_MAXIMIZE = 3;
        private const int SW_SHOW = 5;

        private delegate bool EnumMonitorsDelegate(IntPtr hMonitor, IntPtr hdcMonitor, ref RECT lprcMonitor, IntPtr dwData);
        private delegate IntPtr LowLevelMouseProc(int nCode, IntPtr wParam, IntPtr lParam);

        [StructLayout(LayoutKind.Sequential)] public struct Point { public int x; public int y; }
        [StructLayout(LayoutKind.Sequential)] public struct MSLLHOOKSTRUCT { public Point pt; public uint mouseData; public uint flags; public uint time; public IntPtr dwExtraInfo; }
        [StructLayout(LayoutKind.Sequential)] public struct RECT { public int Left; public int Top; public int Right; public int Bottom; }
        [StructLayout(LayoutKind.Sequential)] public struct MONITORINFO { public uint cbSize; public RECT rcMonitor; public RECT rcWork; public uint dwFlags; }
        #endregion

        [STAThread]
        static void Main()
        {
            try { SetProcessDpiAwarenessContext(DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2); } catch { }

            using (Mutex mutex = new Mutex(false, "Global\\" + "MyWindowMoveTool_Unique_Id"))
            {
                if (!mutex.WaitOne(0, false))
                {
                    MessageBox.Show("程序已在运行中！", "Window Mover", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);

                hookProc = HookCallback;
                SetHookEnabled(true);

                Icon appIcon = IconGenerator.GenerateAppIcon();
                Application.Run(new MainForm(appIcon));
                
                SetHookEnabled(false);
            }
        }

        public static void SetHookEnabled(bool enabled)
        {
            if (enabled && hookId == IntPtr.Zero)
            {
                using (Process curProcess = Process.GetCurrentProcess())
                using (ProcessModule curModule = curProcess.MainModule)
                {
                    hookId = SetWindowsHookEx(WH_MOUSE_LL, hookProc, GetModuleHandle(curModule.ModuleName), 0);
                }
                isHookEnabled = true;
            }
            else if (!enabled && hookId != IntPtr.Zero)
            {
                UnhookWindowsHookEx(hookId);
                hookId = IntPtr.Zero;
                isHookEnabled = false;
            }
        }

        private static IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode < 0) return CallNextHookEx(hookId, nCode, wParam, lParam);

            if (isHookEnabled && wParam == (IntPtr)WM_MBUTTONDOWN)
            {
                MSLLHOOKSTRUCT hookStruct = (MSLLHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(MSLLHOOKSTRUCT));
                try
                {
                    IntPtr hwnd = WindowFromPoint(hookStruct.pt);
                    hwnd = GetAncestor(hwnd, GA_ROOT);

                    if (hwnd != IntPtr.Zero)
                    {
                        IntPtr hitResultPtr;
                        IntPtr lParamCoords = (IntPtr)((hookStruct.pt.y << 16) | (hookStruct.pt.x & 0xFFFF));
                        IntPtr result = SendMessageTimeout(hwnd, WM_NCHITTEST, IntPtr.Zero, lParamCoords, SMTO_ABORTIFHUNG, 200, out hitResultPtr);

                        long hitValue = (result != IntPtr.Zero) ? hitResultPtr.ToInt64() : 0;
                        bool shouldMove = false;
                        string processName = GetProcessName(hwnd);

                        // 调试日志
                        System.Diagnostics.Debug.WriteLine($"[WindowMover] Process: {processName}, HitTest: {hitValue}, Point: ({hookStruct.pt.x}, {hookStruct.pt.y})");

                        // ==============================================================
                        // 资源管理器特殊处理：使用 UI Automation 精确检测标签页
                        // ==============================================================
                        if (processName == "explorer")
                        {
                            // 使用 UI Automation 检测是否在标签页上
                            bool isOverTab = IsOverExplorerTab(hookStruct.pt);
                            
                            System.Diagnostics.Debug.WriteLine($"[WindowMover] Explorer UI Automation check - IsOverTab: {isOverTab}");
                            
                            // 如果在标签页上，不移动（让系统处理关闭标签）
                            if (isOverTab)
                            {
                                shouldMove = false;
                            }
                            // 否则检查是否在标题栏区域（使用几何判定）
                            else
                            {
                                RECT winRect;
                                GetWindowRect(hwnd, out winRect);
                                int titleBarHeight = 45;
                                
                                bool isInTopArea = (hookStruct.pt.y >= winRect.Top) && (hookStruct.pt.y <= winRect.Top + titleBarHeight);
                                bool isInsideWidth = (hookStruct.pt.x >= winRect.Left) && (hookStruct.pt.x <= winRect.Right);
                                
                                System.Diagnostics.Debug.WriteLine($"[WindowMover] Explorer geometry check - IsInTopArea: {isInTopArea}, IsInsideWidth: {isInsideWidth}");
                                
                                if (isInTopArea && isInsideWidth)
                                {
                                    shouldMove = true;
                                }
                            }
                        }
                        // ==============================================================
                        // 其他窗口：标准判定
                        // ==============================================================
                        else if (hitValue == HTCAPTION)
                        {
                            shouldMove = true;
                        }
                        // ==============================================================
                        // 特殊判定：VS Code 和任务管理器（不认标题栏的应用）
                        // ==============================================================
                        else if (processName == "code" || processName == "taskmgr")
                        {
                            RECT winRect;
                            GetWindowRect(hwnd, out winRect);
                            int titleBarHeight = 45;
                            
                            bool isInTopArea = (hookStruct.pt.y >= winRect.Top) && (hookStruct.pt.y <= winRect.Top + titleBarHeight);
                            bool isInsideWidth = (hookStruct.pt.x >= winRect.Left) && (hookStruct.pt.x <= winRect.Right);

                            if (isInTopArea && isInsideWidth) shouldMove = true;
                        }

                        if (shouldMove)
                        {
                            MoveWindowToNextMonitor(hwnd);
                            return (IntPtr)1;
                        }
                    }
                }
                catch { }
            }
            return CallNextHookEx(hookId, nCode, wParam, lParam);
        }

        private static string GetProcessName(IntPtr hwnd)
        {
            try
            {
                uint pid;
                GetWindowThreadProcessId(hwnd, out pid);
                using (Process p = Process.GetProcessById((int)pid))
                {
                    return p.ProcessName.ToLower();
                }
            }
            catch
            {
                return "";
            }
        }

        // 使用 UI Automation 检测是否在资源管理器标签页上
        private static bool IsOverExplorerTab(Point point)
        {
            try
            {
                // 将 Point 转换为 System.Windows.Point
                var automationPoint = new System.Windows.Point(point.x, point.y);
                
                // 获取指定位置的 UI Automation 元素
                var element = AutomationElement.FromPoint(automationPoint);
                if (element == null) return false;
                
                // 获取元素的控件类型
                var controlType = element.Current.ControlType;
                
                // 获取元素的类名
                var className = element.Current.ClassName;
                
                System.Diagnostics.Debug.WriteLine($"[WindowMover] UI Automation - ControlType: {controlType.ProgrammaticName}, ClassName: {className}");
                
                // 检查是否是标签页相关控件
                // TabItem: 单个标签页
                // Tab: 标签页容器
                // 或者类名包含 "Tab"
                if (controlType == ControlType.TabItem ||
                    controlType == ControlType.Tab ||
                    (className != null && className.Contains("Tab")))
                {
                    return true;
                }
                
                return false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[WindowMover] UI Automation error: {ex.Message}");
                return false;
            }
        }

        private static void MoveWindowToNextMonitor(IntPtr hwnd)
        {
            List<(IntPtr handle, MONITORINFO info)> monitors = new List<(IntPtr, MONITORINFO)>();
            EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, (IntPtr hMonitor, IntPtr hdcMonitor, ref RECT lprcMonitor, IntPtr dwData) =>
            {
                MONITORINFO mi = new MONITORINFO { cbSize = (uint)Marshal.SizeOf(typeof(MONITORINFO)) };
                GetMonitorInfo(hMonitor, ref mi);
                monitors.Add((hMonitor, mi));
                return true;
            }, IntPtr.Zero);

            if (monitors.Count <= 1) return;

            IntPtr currentMonitorHandle = MonitorFromWindow(hwnd, 2);
            int currentIndex = monitors.FindIndex(m => m.handle == currentMonitorHandle);
            if (currentIndex == -1) currentIndex = 0;

            int nextIndex = (currentIndex + 1) % monitors.Count;
            MONITORINFO currentMonitor = monitors[currentIndex].info;
            MONITORINFO nextMonitor = monitors[nextIndex].info;

            bool isMaximized = IsZoomed(hwnd);
            if (isMaximized)
            {
                ShowWindow(hwnd, SW_RESTORE);
                Thread.Sleep(50); 
            }

            RECT windowRect;
            GetWindowRect(hwnd, out windowRect);
            int width = windowRect.Right - windowRect.Left;
            int height = windowRect.Bottom - windowRect.Top;

            double relX = (double)(windowRect.Left - currentMonitor.rcMonitor.Left) / (currentMonitor.rcMonitor.Right - currentMonitor.rcMonitor.Left);
            double relY = (double)(windowRect.Top - currentMonitor.rcMonitor.Top) / (currentMonitor.rcMonitor.Bottom - currentMonitor.rcMonitor.Top);
            
            int newX = nextMonitor.rcMonitor.Left + (int)(relX * (nextMonitor.rcMonitor.Right - nextMonitor.rcMonitor.Left));
            int newY = nextMonitor.rcMonitor.Top + (int)(relY * (nextMonitor.rcMonitor.Bottom - nextMonitor.rcMonitor.Top));

            if (newX < nextMonitor.rcWork.Left) newX = nextMonitor.rcWork.Left;
            if (newY < nextMonitor.rcWork.Top) newY = nextMonitor.rcWork.Top;

            int workWidth = nextMonitor.rcWork.Right - nextMonitor.rcWork.Left;
            int workHeight = nextMonitor.rcWork.Bottom - nextMonitor.rcWork.Top;

            int finalWidth = (width > workWidth) ? workWidth : width;
            int finalHeight = (height > workHeight) ? workHeight : height;

            SetWindowPos(hwnd, HWND_TOP, newX, newY, finalWidth, finalHeight, SWP_SHOWWINDOW | SWP_NOCOPYBITS | SWP_FRAMECHANGED);

            if (!isMaximized)
            {
                SetWindowPos(hwnd, IntPtr.Zero, 0, 0, finalWidth + 1, finalHeight, 0x0004 | SWP_NOMOVE | SWP_NOCOPYBITS);
                SetWindowPos(hwnd, IntPtr.Zero, 0, 0, finalWidth, finalHeight, 0x0004 | SWP_NOMOVE | SWP_NOCOPYBITS);
            }
            else
            {
                ShowWindow(hwnd, SW_MAXIMIZE);
            }

            ForceForegroundWindow(hwnd);
        }

        private static void ForceForegroundWindow(IntPtr hwnd)
        {
            if (IsIconic(hwnd)) ShowWindow(hwnd, SW_RESTORE);
            
            uint foreThread = GetWindowThreadProcessId(GetForegroundWindow(), IntPtr.Zero);
            uint appThread = GetCurrentThreadId();
            uint targetThread = GetWindowThreadProcessId(hwnd, IntPtr.Zero);

            if (foreThread != targetThread)
            {
                AttachThreadInput(foreThread, appThread, true);
                AttachThreadInput(targetThread, appThread, true);
                BringWindowToTop(hwnd);
                ShowWindow(hwnd, SW_SHOW);
                SetForegroundWindow(hwnd);
                AttachThreadInput(targetThread, appThread, false);
                AttachThreadInput(foreThread, appThread, false);
            }
            else
            {
                BringWindowToTop(hwnd);
                ShowWindow(hwnd, SW_SHOW);
                SetForegroundWindow(hwnd);
            }
        }
    }

    public static class IconGenerator
    {
        public static Icon GenerateAppIcon()
        {
            int size = 64; 
            using (Bitmap bitmap = new Bitmap(size, size))
            using (Graphics g = Graphics.FromImage(bitmap))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;
                Color primaryColor = Color.FromArgb(0, 120, 215);
                using (SolidBrush bgBrush = new SolidBrush(primaryColor)) g.FillEllipse(bgBrush, 0, 0, size, size);
                using (Pen arrowPen = new Pen(Color.White, 5f))
                {
                    arrowPen.StartCap = LineCap.Round; arrowPen.EndCap = LineCap.Round;
                    int center = size / 2; int offsetShort = 8; int offsetLong = 18;
                    g.DrawLine(arrowPen, center, center - offsetLong, center, center - offsetShort);
                    g.DrawLine(arrowPen, center, center - offsetLong, center - 6, center - offsetLong + 6);
                    g.DrawLine(arrowPen, center, center - offsetLong, center + 6, center - offsetLong + 6);
                    g.DrawLine(arrowPen, center, center + offsetLong, center, center + offsetShort);
                    g.DrawLine(arrowPen, center, center + offsetLong, center - 6, center + offsetLong - 6);
                    g.DrawLine(arrowPen, center, center + offsetLong, center + 6, center + offsetLong - 6);
                    g.DrawLine(arrowPen, center - offsetLong, center, center - offsetShort, center);
                    g.DrawLine(arrowPen, center - offsetLong, center, center - offsetLong + 6, center - 6);
                    g.DrawLine(arrowPen, center - offsetLong, center, center - offsetLong + 6, center + 6);
                    g.DrawLine(arrowPen, center + offsetLong, center, center + offsetShort, center);
                    g.DrawLine(arrowPen, center + offsetLong, center, center + offsetLong - 6, center - 6);
                    g.DrawLine(arrowPen, center + offsetLong, center, center + offsetLong - 6, center + 6);
                }
                using (SolidBrush centerBrush = new SolidBrush(Color.White)) g.FillEllipse(centerBrush, size / 2 - 4, size / 2 - 4, 8, 8);
                return Icon.FromHandle(bitmap.GetHicon());
            }
        }
    }
}