namespace 快捷键窗体切换
{
    public class CustomWindowPosition
    {
        public int X { get; set; }
        public int Y { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public int VirtualDesktopIndex { get; set; }  // 虚拟桌面索引
    }

    public class HotkeyConfig
    {
        public bool IsControl { get; set; }
        public bool IsAlt { get; set; }
        public bool IsShift { get; set; }
        public bool IsWin { get; set; }
        public uint Key { get; set; }
        public string ProcessName { get; set; }
        public string AppPath { get; set; }
        public bool IsHotkeyMapping { get; set; }
        public HotkeyConfig MappedHotkey { get; set; }
        public CustomWindowPosition Position { get; set; }  // 窗口位置信息
    }

    public class SavedHotkeyConfig
    {
        public bool IsControl { get; set; }
        public bool IsAlt { get; set; }
        public bool IsShift { get; set; }
        public bool IsWin { get; set; }
        public uint Key { get; set; }
        public string ProcessName { get; set; }
        public string AppPath { get; set; }
        public bool IsHotkeyMapping { get; set; }
        public SavedHotkeyConfig MappedHotkey { get; set; }
        public CustomWindowPosition Position { get; set; }  // 窗口位置信息
    }
} 