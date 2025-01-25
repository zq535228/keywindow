using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Text.Json;
using System.IO;
using System.Diagnostics;
using System.Linq;
using System.Drawing;
using System.Drawing.Imaging;
using Microsoft.Win32;  // 添加注册表命名空间

namespace 快捷键窗体切换
{
    public partial class MainForm : Form
    {
        private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        // Windows API 导入
        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

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

        [DllImport("shell32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr ExtractIcon(IntPtr hInst, string lpszExeFileName, int nIconIndex);

        [DllImport("user32.dll")]
        private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);  // 添加模拟按键的API

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        // 修饰键定义
        private const uint MOD_ALT = 0x0001;
        private const uint MOD_CONTROL = 0x0002;
        private const uint MOD_SHIFT = 0x0004;
        private const uint MOD_WIN = 0x0008;

        private const uint SWP_SHOWWINDOW = 0x0040;
        private const uint SWP_NOSIZE = 0x0001;

        private const int SW_RESTORE = 9;

        private const uint KEYEVENTF_KEYDOWN = 0x0000;
        private const uint KEYEVENTF_KEYUP = 0x0002;

        // 保存快捷键配置的字典
        private Dictionary<int, HotkeyConfig> hotkeyConfigs = new Dictionary<int, HotkeyConfig>();
        private int currentHotkeyId = 1;
        private NotifyIcon trayIcon;
        private ListView listView;
        private const string CONFIG_FILE = "hotkeys.json";

        private ListViewItem draggedItem;
        private int draggedItemIndex;

        public MainForm()
        {
            InitializeComponent();
            this.FormClosing += MainForm_FormClosing;
            InitializeUI();
            LoadConfig();
        }

        private void InitializeUI()
        {
            this.Size = new System.Drawing.Size(350, 500);  // 设置窗口大小
            this.Text = "快捷键窗口切换工具";
            this.StartPosition = FormStartPosition.CenterScreen;
            this.MaximizeBox = false;
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.TopMost = true;

            // 创建一个按钮用于添加新的快捷键配置
            Button addButton = new Button
            {
                Text = "添加快捷键",
                Location = new System.Drawing.Point(140, 10),
                Size = new System.Drawing.Size(120, 30)
            };
            addButton.Click += AddButton_Click;
            this.Controls.Add(addButton);
            
            // 创建一个按钮用于启动所有应用
            Button startAllButton = new Button
            {
                Text = "一键启动",
                Location = new System.Drawing.Point(10, 10),
                Size = new System.Drawing.Size(120, 30)
            };
            startAllButton.Click += StartAllButton_Click;
            this.Controls.Add(startAllButton);

            // 创建ListView并设置其占满剩余空间
            listView = new ListView
            {
                Location = new System.Drawing.Point(10, 50),
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                View = View.Details,
                FullRowSelect = true,
                GridLines = true,
                SmallImageList = new ImageList(),
                AllowDrop = true
            };
            // 设置ListView的大小为窗口大小减去边距
            listView.Size = new System.Drawing.Size(
                this.ClientSize.Width - 20,  // 左右各留10像素边距
                this.ClientSize.Height - 60  // 上面留50像素，下面留10像素边距
            );

            // 设置图标列表的属性
            listView.SmallImageList.ColorDepth = ColorDepth.Depth32Bit;
            listView.SmallImageList.ImageSize = new Size(16, 16);

            // 添加列，保持原有列宽
            listView.Columns.Add("ID", 40);
            listView.Columns.Add("应用名称", 110);
            listView.Columns.Add("快捷键", 120);
            listView.Columns.Add("启动路径", 0);  // 宽度设为0来隐藏启动路径列

            // 添加列宽改变事件处理
            listView.ColumnWidthChanging += ListView_ColumnWidthChanging;

            // 添加拖拽排序相关事件
            listView.ItemDrag += ListView_ItemDrag;
            listView.DragEnter += ListView_DragEnter;
            listView.DragOver += ListView_DragOver;
            listView.DragDrop += ListView_DragDrop;

            listView.DoubleClick += ListView_DoubleClick;
            listView.MouseClick += ListView_MouseClick;

            this.Controls.Add(listView);

            // 创建托盘图标
            trayIcon = new NotifyIcon
            {
                Text = "快捷键窗口切换工具",
                Visible = true
            };

            // 尝试加载自定义图标
            string icoPath = "x.ico";
            string pngPath = "x.png";
            
            if (File.Exists(icoPath))
            {
                trayIcon.Icon = new Icon(icoPath);
            }
            else if (File.Exists(pngPath))
            {
                using (var bitmap = new Bitmap(pngPath))
                {
                    trayIcon.Icon = Icon.FromHandle(bitmap.GetHicon());
                }
            }
            else
            {
                trayIcon.Icon = System.Drawing.SystemIcons.Application;  // 默认图标
            }

            trayIcon.Click += TrayIcon_Click;  // 添加单击事件
            trayIcon.DoubleClick += TrayIcon_DoubleClick;

            // 创建托盘图标的右键菜单
            ContextMenuStrip trayMenu = new ContextMenuStrip();
            ToolStripMenuItem showItem = new ToolStripMenuItem("显示");
            showItem.Click += (s, e) => ShowMainWindow();
            trayMenu.Items.Add(showItem);

            // 添加开机自启动选项
            ToolStripMenuItem autoStartItem = new ToolStripMenuItem("开机自启动");
            autoStartItem.Checked = IsAutoStartEnabled();
            autoStartItem.Click += AutoStartItem_Click;
            trayMenu.Items.Add(autoStartItem);

            ToolStripMenuItem exitItem = new ToolStripMenuItem("退出");
            exitItem.Click += (s, e) => { Application.Exit(); };
            trayMenu.Items.Add(exitItem);

            trayIcon.ContextMenuStrip = trayMenu;
        }

        private void DeleteButton_Click(object sender, EventArgs e)
        {
            if (listView.SelectedItems.Count > 0)
            {
                ListViewItem item = listView.SelectedItems[0];
                int hotkeyId = int.Parse(item.Text);
                
                // 注销快捷键
                UnregisterHotKey(this.Handle, hotkeyId);
                
                // 从字典和列表中移除
                hotkeyConfigs.Remove(hotkeyId);
                item.Remove();
            }
        }

        private string GetHotkeyText(HotkeyConfig config)
        {
            var parts = new List<string>();
            if (config.IsControl) parts.Add("Ctrl");
            if (config.IsAlt) parts.Add("Alt");
            if (config.IsShift) parts.Add("Shift");
            if (config.IsWin) parts.Add("Win");
            parts.Add(((Keys)config.Key).ToString());
            return string.Join(" + ", parts);
        }

        private void AddButton_Click(object sender, EventArgs e)
        {
            this.TopMost = false;  // 暂时取消主窗口置顶
            using (var configForm = new HotkeyConfigForm())
            {
                if (configForm.ShowDialog() == DialogResult.OK)
                {
                    var config = configForm.GetConfig();
                    if (config != null)
                    {
                        RegisterNewHotkey(config);
                    }
                }
            }
            this.TopMost = true;  // 恢复主窗口置顶
        }

        private Icon GetFileIcon(string path)
        {
            try
            {
                if (string.IsNullOrEmpty(path))
                    return null;

                if (File.Exists(path))
                {
                    // 尝试从文件中提取图标
                    IntPtr hIcon = ExtractIcon(Process.GetCurrentProcess().Handle, path, 0);
                    if (hIcon != IntPtr.Zero)
                    {
                        return Icon.FromHandle(hIcon);
                    }
                }

                // 如果无法从文件中提取图标，尝试从运行中的进程获取
                var processName = Path.GetFileNameWithoutExtension(path);
                var process = Process.GetProcessesByName(processName).FirstOrDefault();
                if (process != null && !string.IsNullOrEmpty(process.MainModule?.FileName))
                {
                    return Icon.ExtractAssociatedIcon(process.MainModule.FileName);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"获取图标失败: {ex.Message}");
            }
            return null;
        }

        private void RegisterNewHotkey(HotkeyConfig config)
        {
            uint modifiers = 0;
            if (config.IsAlt) modifiers |= MOD_ALT;
            if (config.IsControl) modifiers |= MOD_CONTROL;
            if (config.IsShift) modifiers |= MOD_SHIFT;
            if (config.IsWin) modifiers |= MOD_WIN;

            if (RegisterHotKey(this.Handle, currentHotkeyId, modifiers, config.Key))
            {
                hotkeyConfigs.Add(currentHotkeyId, config);
                
                // 添加到ListView
                var item = new ListViewItem(currentHotkeyId.ToString());
                
                // 尝试获取应用程序图标
                try
                {
                    string appPath = config.AppPath;
                    if (string.IsNullOrEmpty(appPath))
                    {
                        // 尝试从运行中的进程获取图标
                        var process = Process.GetProcessesByName(config.ProcessName).FirstOrDefault();
                        if (process != null && !string.IsNullOrEmpty(process.MainModule?.FileName))
                        {
                            appPath = process.MainModule.FileName;
                        }
                    }

                    if (!string.IsNullOrEmpty(appPath))
                    {
                        Icon appIcon = GetFileIcon(appPath);
                        if (appIcon != null)
                        {
                            string imageKey = $"img_{currentHotkeyId}";
                            if (!listView.SmallImageList.Images.ContainsKey(imageKey))
                            {
                                listView.SmallImageList.Images.Add(imageKey, appIcon);
                            }
                            item.ImageKey = imageKey;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"获取图标失败: {ex.Message}");
                }

                item.SubItems.Add(config.ProcessName);  // 应用名称
                item.SubItems.Add(GetHotkeyText(config));  // 快捷键
                item.SubItems.Add(config.AppPath ?? "");  // 启动路径（隐藏列）
                listView.Items.Add(item);
                
                currentHotkeyId++;
            }
            else
            {
                MessageBox.Show("注册快捷键失败！");
            }
        }

        protected override void WndProc(ref Message m)
        {
            const int WM_HOTKEY = 0x0312;

            if (m.Msg == WM_HOTKEY)
            {
                int hotkeyId = m.WParam.ToInt32();
                if (hotkeyConfigs.TryGetValue(hotkeyId, out HotkeyConfig config))
                {
                    // 如果是快捷键映射，使用空字符串作为进程名
                    string processName = config.IsHotkeyMapping ? hotkeyId.ToString() : config.ProcessName;
                    SwitchToWindow(processName);
                }
            }

            base.WndProc(ref m);
        }

        private void SwitchToWindow(string processName)
        {
            try
            {
                // 检查是否是快捷键映射
                var mappingConfig = hotkeyConfigs.Values.FirstOrDefault(c => 
                    (c.IsHotkeyMapping && processName == hotkeyConfigs.FirstOrDefault(x => x.Value == c).Key.ToString()) ||
                    (!c.IsHotkeyMapping && c.ProcessName == processName));

                if (mappingConfig != null && mappingConfig.IsHotkeyMapping && mappingConfig.MappedHotkey != null)
                {
                    // 模拟按下映射的快捷键
                    SimulateHotkey(mappingConfig.MappedHotkey);
                    // 添加一个小延时，确保快捷键能被正确处理
                    System.Threading.Thread.Sleep(10);
                    return;  // 执行完映射后直接返回
                }

                // 如果不是快捷键映射，则尝试切换窗口
                Process[] processes = Process.GetProcessesByName(processName);
                if (processes.Length > 0)
                {
                    // 找到所有匹配进程的主窗口
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

                        // 如果窗口是最小化的，先恢复它
                        if (IsIconic(hWnd))
                        {
                            ShowWindow(hWnd, SW_RESTORE);
                        }

                        // 切换到窗口
                        SetForegroundWindow(hWnd);
                    }
                }
                else
                {
                    // 尝试启动应用程序
                    try
                    {
                        // 获取当前快捷键配置
                        var appConfig = hotkeyConfigs.Values.FirstOrDefault(c => c.ProcessName == processName);
                        if (appConfig != null && !string.IsNullOrEmpty(appConfig.AppPath))
                        {
                            // 启动应用程序
                            var process = Process.Start(new ProcessStartInfo
                            {
                                FileName = appConfig.AppPath,
                                UseShellExecute = true
                            });

                            if (process != null)
                            {
                                // 等待窗口创建
                                process.WaitForInputIdle(5000);
                                if (process.MainWindowHandle != IntPtr.Zero)
                                {
                                    // 设置窗口位置
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

                            string startCommand = processName;
                            if (appPaths.ContainsKey(processName))
                            {
                                startCommand = appPaths[processName];
                            }

                            var process = Process.Start(new ProcessStartInfo
                            {
                                FileName = startCommand,
                                UseShellExecute = true
                            });

                            // 对于默认应用也尝试设置屏幕位置
                            if (process != null)
                            {
                                process.WaitForInputIdle(5000);
                                if (process.MainWindowHandle != IntPtr.Zero)
                                {
                                    // 获取窗口大小并设置在屏幕中心
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
                    catch (Exception ex)
                    {
                        MessageBox.Show($"无法启动应用程序: {ex.Message}\n" +
                                      $"进程名称: {processName}");
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"切换窗口失败: {ex.Message}");
            }
        }

        private void SimulateHotkey(HotkeyConfig mappedHotkey)
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
                System.Threading.Thread.Sleep(50);  // 等待50毫秒
                keybd_event((byte)mappedHotkey.Key, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
                System.Threading.Thread.Sleep(50);  // 等待50毫秒

                // 释放修饰键
                if (mappedHotkey.IsWin)
                    keybd_event((byte)Keys.LWin, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
                if (mappedHotkey.IsShift)
                    keybd_event((byte)Keys.ShiftKey, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
                if (mappedHotkey.IsAlt)
                    keybd_event((byte)Keys.Menu, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
                if (mappedHotkey.IsControl)
                    keybd_event((byte)Keys.ControlKey, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);

                // 等待一小段时间，确保按键事件被处理
                System.Threading.Thread.Sleep(100);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"模拟按键失败: {ex.Message}");
            }
        }

        private void LoadConfig()
        {
            try
            {
                // 初始化图标列表
                listView.SmallImageList = new ImageList
                {
                    ColorDepth = ColorDepth.Depth32Bit,
                    ImageSize = new Size(16, 16)
                };

                if (File.Exists(CONFIG_FILE))
                {
                    string jsonString = File.ReadAllText(CONFIG_FILE);
                    var configs = JsonSerializer.Deserialize<List<SavedHotkeyConfig>>(jsonString);
                    
                    // 清空现有配置
                    listView.Items.Clear();
                    foreach (int hotkeyId in hotkeyConfigs.Keys.ToList())
                    {
                        UnregisterHotKey(this.Handle, hotkeyId);
                    }
                    hotkeyConfigs.Clear();
                    currentHotkeyId = 1;

                    // 按保存的顺序加载配置
                    foreach (var savedConfig in configs)
                    {
                        var config = new HotkeyConfig
                        {
                            IsControl = savedConfig.IsControl,
                            IsAlt = savedConfig.IsAlt,
                            IsShift = savedConfig.IsShift,
                            IsWin = savedConfig.IsWin,
                            Key = savedConfig.Key,
                            ProcessName = savedConfig.ProcessName,
                            AppPath = savedConfig.AppPath,
                            IsHotkeyMapping = savedConfig.IsHotkeyMapping
                        };

                        if (savedConfig.MappedHotkey != null)
                        {
                            config.MappedHotkey = new HotkeyConfig
                            {
                                IsControl = savedConfig.MappedHotkey.IsControl,
                                IsAlt = savedConfig.MappedHotkey.IsAlt,
                                IsShift = savedConfig.MappedHotkey.IsShift,
                                IsWin = savedConfig.MappedHotkey.IsWin,
                                Key = savedConfig.MappedHotkey.Key
                            };
                        }

                        RegisterNewHotkey(config);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"加载配置失败: {ex.Message}");
            }
        }

        private void SaveConfig()
        {
            try
            {
                var configs = new List<SavedHotkeyConfig>();
                
                // 按ListView中的顺序保存配置
                foreach (ListViewItem item in listView.Items)
                {
                    int hotkeyId = int.Parse(item.Text);
                    if (hotkeyConfigs.TryGetValue(hotkeyId, out HotkeyConfig config))
                    {
                        var savedConfig = new SavedHotkeyConfig
                        {
                            IsControl = config.IsControl,
                            IsAlt = config.IsAlt,
                            IsShift = config.IsShift,
                            IsWin = config.IsWin,
                            Key = config.Key,
                            ProcessName = config.ProcessName,
                            AppPath = config.AppPath,
                            IsHotkeyMapping = config.IsHotkeyMapping
                        };

                        if (config.MappedHotkey != null)
                        {
                            savedConfig.MappedHotkey = new SavedHotkeyConfig
                            {
                                IsControl = config.MappedHotkey.IsControl,
                                IsAlt = config.MappedHotkey.IsAlt,
                                IsShift = config.MappedHotkey.IsShift,
                                IsWin = config.MappedHotkey.IsWin,
                                Key = config.MappedHotkey.Key
                            };
                        }

                        configs.Add(savedConfig);
                    }
                }

                string jsonString = JsonSerializer.Serialize(configs, new JsonSerializerOptions 
                { 
                    WriteIndented = true 
                });
                File.WriteAllText(CONFIG_FILE, jsonString);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"保存配置失败: {ex.Message}");
            }
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            // 保存配置
            SaveConfig();

            // 注销所有快捷键
            foreach (int hotkeyId in hotkeyConfigs.Keys)
            {
                UnregisterHotKey(this.Handle, hotkeyId);
            }
            
            // 清理托盘图标
            if (trayIcon != null)
            {
                trayIcon.Dispose();
            }
        }

        private void MinimizeButton_Click(object sender, EventArgs e)
        {
            this.Hide();
        }

        private void ShowMainWindow()
        {
            this.Show();
            this.WindowState = FormWindowState.Normal;
            this.Activate();  // 确保窗口获得焦点
        }

        private void TrayIcon_Click(object sender, EventArgs e)
        {
            // 检查是否是左键单击
            if (((MouseEventArgs)e).Button == MouseButtons.Left)
            {
                ShowMainWindow();
            }
        }

        private void TrayIcon_DoubleClick(object sender, EventArgs e)
        {
            ShowMainWindow();
        }

        private void ListView_DoubleClick(object sender, EventArgs e)
        {
            if (listView.SelectedItems.Count > 0)
            {
                ListViewItem item = listView.SelectedItems[0];
                int hotkeyId = int.Parse(item.Text);
                if (hotkeyConfigs.TryGetValue(hotkeyId, out HotkeyConfig config))
                {
                    SwitchToWindow(config.ProcessName);
                }
            }
        }

        private void ListView_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right && listView.SelectedItems.Count > 0)
            {
                // 创建右键菜单
                ContextMenuStrip menu = new ContextMenuStrip();
                
                // 添加编辑选项
                ToolStripMenuItem editItem = new ToolStripMenuItem("编辑");
                editItem.Click += EditMenuItem_Click;
                menu.Items.Add(editItem);

                // 添加删除选项
                ToolStripMenuItem deleteItem = new ToolStripMenuItem("删除");
                deleteItem.Click += DeleteMenuItem_Click;
                menu.Items.Add(deleteItem);
                
                // 在鼠标位置显示菜单
                menu.Show(listView, e.Location);
            }
        }

        private void EditMenuItem_Click(object sender, EventArgs e)
        {
            if (listView.SelectedItems.Count > 0)
            {
                ListViewItem item = listView.SelectedItems[0];
                int hotkeyId = int.Parse(item.Text);
                if (hotkeyConfigs.TryGetValue(hotkeyId, out HotkeyConfig config))
                {
                    this.TopMost = false;  // 暂时取消主窗口置顶
                    using (var configForm = new HotkeyConfigForm())
                    {
                        // 设置当前配置
                        configForm.SetConfig(config);

                        if (configForm.ShowDialog() == DialogResult.OK)
                        {
                            // 获取新的配置
                            var newConfig = configForm.GetConfig();
                            if (newConfig != null)
                            {
                                // 先注册新的快捷键，使用一个临时ID
                                int tempHotkeyId = currentHotkeyId;
                                uint modifiers = 0;
                                if (newConfig.IsAlt) modifiers |= MOD_ALT;
                                if (newConfig.IsControl) modifiers |= MOD_CONTROL;
                                if (newConfig.IsShift) modifiers |= MOD_SHIFT;
                                if (newConfig.IsWin) modifiers |= MOD_WIN;

                                // 新快捷键注册成功后，再注销旧的快捷键
                                UnregisterHotKey(this.Handle , hotkeyId);

                                if (RegisterHotKey(this.Handle, tempHotkeyId, modifiers, newConfig.Key))
                                {
                                    // 更新配置
                                    hotkeyConfigs.Remove(hotkeyId);
                                    hotkeyConfigs.Add(tempHotkeyId, newConfig);

                                    // 更新列表项
                                    item.Text = tempHotkeyId.ToString();
                                    item.SubItems[1].Text = newConfig.ProcessName;
                                    item.SubItems[2].Text = GetHotkeyText(newConfig);
                                    item.SubItems[3].Text = newConfig.AppPath ?? "";

                                    // 更新图标
                                    try
                                    {
                                        string appPath = newConfig.AppPath;
                                        if (string.IsNullOrEmpty(appPath))
                                        {
                                            var process = Process.GetProcessesByName(newConfig.ProcessName).FirstOrDefault();
                                            if (process != null && !string.IsNullOrEmpty(process.MainModule?.FileName))
                                            {
                                                appPath = process.MainModule.FileName;
                                            }
                                        }

                                        if (!string.IsNullOrEmpty(appPath))
                                        {
                                            Icon appIcon = GetFileIcon(appPath);
                                            if (appIcon != null)
                                            {
                                                string oldImageKey = $"img_{hotkeyId}";
                                                string newImageKey = $"img_{tempHotkeyId}";
                                                
                                                if (listView.SmallImageList.Images.ContainsKey(oldImageKey))
                                                {
                                                    listView.SmallImageList.Images.RemoveByKey(oldImageKey);
                                                }
                                                listView.SmallImageList.Images.Add(newImageKey, appIcon);
                                                item.ImageKey = newImageKey;
                                            }
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        Debug.WriteLine($"更新图标失败: {ex.Message}");
                                    }

                                    // 更新当前快捷键ID
                                    currentHotkeyId++;

                                    // 保存配置到JSON文件
                                    SaveConfig();
                                }
                                else
                                {
                                    MessageBox.Show("更新快捷键失败！");
                                }
                            }
                        }
                    }
                    this.TopMost = true;  // 恢复主窗口置顶
                }
            }
        }

        private void DeleteMenuItem_Click(object sender, EventArgs e)
        {
            if (listView.SelectedItems.Count > 0)
            {
                ListViewItem item = listView.SelectedItems[0];
                int hotkeyId = int.Parse(item.Text);
                if (UnregisterHotKey(this.Handle, hotkeyId))
                {
                    hotkeyConfigs.Remove(hotkeyId);  // 从配置字典中移除
                    item.Remove();
                    SaveConfig();  // 保存配置到JSON文件
                }
            }
        }

        private void StartAllButton_Click(object sender, EventArgs e)
        {
            foreach (var config in hotkeyConfigs.Values)
            {
                // 检查进程是否已经运行
                var processes = Process.GetProcessesByName(config.ProcessName);
                if (processes.Length == 0)
                {
                    // 进程未运行，启动它
                    SwitchToWindow(config.ProcessName);
                }
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;  // 取消关闭
                this.Hide();  // 隐藏窗口
            }
            else
            {
                base.OnFormClosing(e);
            }
        }

        private void ListView_ColumnWidthChanging(object sender, ColumnWidthChangingEventArgs e)
        {
            e.Cancel = true;
            e.NewWidth = listView.Columns[e.ColumnIndex].Width;
        }

        private void ListView_ItemDrag(object sender, ItemDragEventArgs e)
        {
            draggedItem = (ListViewItem)e.Item;
            draggedItemIndex = draggedItem.Index;
            listView.DoDragDrop(draggedItem, DragDropEffects.Move);
        }

        private void ListView_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(typeof(ListViewItem)))
            {
                e.Effect = DragDropEffects.Move;
            }
        }

        private void ListView_DragOver(object sender, DragEventArgs e)
        {
            Point targetPoint = listView.PointToClient(new Point(e.X, e.Y));
            ListViewItem targetItem = listView.GetItemAt(targetPoint.X, targetPoint.Y);

            if (targetItem != null)
            {
                e.Effect = DragDropEffects.Move;
            }
        }

        private void ListView_DragDrop(object sender, DragEventArgs e)
        {
            Point targetPoint = listView.PointToClient(new Point(e.X, e.Y));
            ListViewItem targetItem = listView.GetItemAt(targetPoint.X, targetPoint.Y);

            if (targetItem != null && draggedItem != null)
            {
                int targetIndex = targetItem.Index;

                // 如果拖动到相同位置，不做任何操作
                if (targetIndex == draggedItemIndex)
                    return;

                // 创建新项并复制属性
                ListViewItem newItem = (ListViewItem)draggedItem.Clone();
                
                // 在目标位置插入新项
                if (targetIndex > draggedItemIndex)
                {
                    listView.Items.Insert(targetIndex + 1, newItem);
                    listView.Items.RemoveAt(draggedItemIndex);
                }
                else
                {
                    listView.Items.Insert(targetIndex, newItem);
                    listView.Items.RemoveAt(draggedItemIndex + 1);
                }

                // 保存新的排序
                SaveConfig();
            }
        }

        private bool IsAutoStartEnabled()
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", true))
                {
                    if (key != null)
                    {
                        string value = (string)key.GetValue(Application.ProductName);
                        return value != null && value.Equals(Application.ExecutablePath, StringComparison.OrdinalIgnoreCase);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"检查自启动状态失败: {ex.Message}");
            }
            return false;
        }

        private void SetAutoStart(bool enable)
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", true))
                {
                    if (key != null)
                    {
                        if (enable)
                        {
                            key.SetValue(Application.ProductName, Application.ExecutablePath);
                        }
                        else
                        {
                            key.DeleteValue(Application.ProductName, false);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"设置自启动{(enable ? "启用" : "禁用")}失败: {ex.Message}");
            }
        }

        private void AutoStartItem_Click(object sender, EventArgs e)
        {
            var menuItem = (ToolStripMenuItem)sender;
            menuItem.Checked = !menuItem.Checked;
            SetAutoStart(menuItem.Checked);
        }
    }
}
