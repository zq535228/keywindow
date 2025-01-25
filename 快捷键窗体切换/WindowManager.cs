using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Windows.Forms;
using System.Drawing;

namespace 快捷键窗体切换
{
    public class WindowManager
    {
        private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool EnumWindows(EnumWindowsProc enumProc, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        [DllImport("user32.dll")]
        private static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        [DllImport("user32.dll")]
        private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll")]
        private static extern bool IsIconic(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);

        private const uint SWP_SHOWWINDOW = 0x0040;
        private const uint SWP_NOSIZE = 0x0001;
        private const int SW_RESTORE = 9;
        private const uint KEYEVENTF_KEYDOWN = 0x0000;
        private const uint KEYEVENTF_KEYUP = 0x0002;

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        public void SimulateHotkey(HotkeyConfig mappedHotkey)
        {
            if (mappedHotkey == null) return;

            try
            {
                // 按下修饰键
                if (mappedHotkey.IsControl)
                    keybd_event((byte)Keys.ControlKey, 0, KEYEVENTF_KEYDOWN, UIntPtr.Zero);
                if (mappedHotkey.IsAlt)
                    keybd_event((byte)Keys.Menu, 0, KEYEVENTF_KEYDOWN, UIntPtr.Zero);
                if (mappedHotkey.IsShift)
                    keybd_event((byte)Keys.ShiftKey, 0, KEYEVENTF_KEYDOWN, UIntPtr.Zero);
                if (mappedHotkey.IsWin)
                    keybd_event((byte)Keys.LWin, 0, KEYEVENTF_KEYDOWN, UIntPtr.Zero);

                // 按下主键并等待一小段时间
                keybd_event((byte)mappedHotkey.Key, 0, KEYEVENTF_KEYDOWN, UIntPtr.Zero);
                System.Threading.Thread.Sleep(50);
                keybd_event((byte)mappedHotkey.Key, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
                System.Threading.Thread.Sleep(50);

                // 释放修饰键
                if (mappedHotkey.IsWin)
                    keybd_event((byte)Keys.LWin, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
                if (mappedHotkey.IsShift)
                    keybd_event((byte)Keys.ShiftKey, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
                if (mappedHotkey.IsAlt)
                    keybd_event((byte)Keys.Menu, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
                if (mappedHotkey.IsControl)
                    keybd_event((byte)Keys.ControlKey, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);

                System.Threading.Thread.Sleep(100);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"模拟按键失败: {ex.Message}");
            }
        }

        public void SwitchToWindow(string processName, Dictionary<int, HotkeyConfig> hotkeyConfigs)
        {
            try
            {
                // 检查是否是快捷键映射
                var mappingConfigs = hotkeyConfigs.Values.Where(c => c.ProcessName == processName && c.IsHotkeyMapping);
                foreach (var mappingConfig in mappingConfigs)
                {
                    SimulateHotkey(mappingConfig.MappedHotkey);
                    System.Threading.Thread.Sleep(10);
                }

                if (!mappingConfigs.Any())
                {
                    Process[] processes = Process.GetProcessesByName(processName);
                    if (processes.Length > 0)
                    {
                        var windows = new List<IntPtr>();
                        EnumWindows((hWnd, lParam) =>
                        {
                            GetWindowThreadProcessId(hWnd, out uint processId);
                            if (IsWindowVisible(hWnd))
                            {
                                foreach (var process in processes)
                                {
                                    if (process.Id == processId && process.MainWindowHandle != IntPtr.Zero)
                                    {
                                        windows.Add(hWnd);
                                        break;
                                    }
                                }
                            }
                            return true;
                        }, IntPtr.Zero);

                        if (windows.Count > 0)
                        {
                            var hWnd = windows[0];
                            if (IsIconic(hWnd))
                            {
                                ShowWindow(hWnd, SW_RESTORE);
                            }
                            SetForegroundWindow(hWnd);
                        }
                    }
                    else
                    {
                        StartApplication(processName, hotkeyConfigs);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"切换窗口失败: {ex.Message}");
            }
        }

        private void StartApplication(string processName, Dictionary<int, HotkeyConfig> hotkeyConfigs)
        {
            try
            {
                var appConfig = hotkeyConfigs.Values.FirstOrDefault(c => c.ProcessName == processName);
                if (appConfig != null && !string.IsNullOrEmpty(appConfig.AppPath))
                {
                    LaunchAndPositionWindow(appConfig.AppPath);
                }
                else
                {
                    // 使用默认路径映射
                    var appPaths = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                    {
                        { "chrome", "chrome" },
                        { "firefox", "firefox" },
                        { "msedge", "msedge" },
                        { "notepad", "notepad" },
                        { "explorer", "explorer" },
                        { "calc", "calc" }
                    };

                    string startCommand = appPaths.ContainsKey(processName) ? appPaths[processName] : processName;
                    LaunchAndPositionWindow(startCommand);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"无法启动应用程序: {ex.Message}\n进程名称: {processName}");
            }
        }

        private void LaunchAndPositionWindow(string command)
        {
            var process = Process.Start(new ProcessStartInfo
            {
                FileName = command,
                UseShellExecute = true
            });

            if (process != null)
            {
                process.WaitForInputIdle(5000);
                if (process.MainWindowHandle != IntPtr.Zero)
                {
                    RECT rect;
                    GetWindowRect(process.MainWindowHandle, out rect);
                    int width = rect.Right - rect.Left;
                    int height = rect.Bottom - rect.Top;
                    int x = (Screen.PrimaryScreen.WorkingArea.Width - width) / 2;
                    int y = (Screen.PrimaryScreen.WorkingArea.Height - height) / 2;

                    SetWindowPos(
                        process.MainWindowHandle,
                        IntPtr.Zero,
                        x, y, width, height,
                        SWP_SHOWWINDOW
                    );

                    SetForegroundWindow(process.MainWindowHandle);
                }
            }
        }
    }
} 