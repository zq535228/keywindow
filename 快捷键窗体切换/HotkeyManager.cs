using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Collections.Generic;

namespace 快捷键窗体切换
{
    public class HotkeyManager
    {
        // Windows API 导入
        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        // 修饰键定义
        public const uint MOD_ALT = 0x0001;
        public const uint MOD_CONTROL = 0x0002;
        public const uint MOD_SHIFT = 0x0004;
        public const uint MOD_WIN = 0x0008;

        private readonly IntPtr _windowHandle;
        private readonly Dictionary<int, HotkeyConfig> _hotkeyConfigs;
        private int _currentHotkeyId = 1;

        public HotkeyManager(IntPtr windowHandle)
        {
            _windowHandle = windowHandle;
            _hotkeyConfigs = new Dictionary<int, HotkeyConfig>();
        }

        public Dictionary<int, HotkeyConfig> HotkeyConfigs => _hotkeyConfigs;

        public bool RegisterHotkey(HotkeyConfig config, out int hotkeyId)
        {
            uint modifiers = 0;
            if (config.IsAlt) modifiers |= MOD_ALT;
            if (config.IsControl) modifiers |= MOD_CONTROL;
            if (config.IsShift) modifiers |= MOD_SHIFT;
            if (config.IsWin) modifiers |= MOD_WIN;

            hotkeyId = _currentHotkeyId;
            if (RegisterHotKey(_windowHandle, _currentHotkeyId, modifiers, config.Key))
            {
                _hotkeyConfigs.Add(_currentHotkeyId, config);
                _currentHotkeyId++;
                return true;
            }
            return false;
        }

        public bool UnregisterHotkey(int hotkeyId)
        {
            if (_hotkeyConfigs.ContainsKey(hotkeyId))
            {
                if (UnregisterHotKey(_windowHandle, hotkeyId))
                {
                    _hotkeyConfigs.Remove(hotkeyId);
                    return true;
                }
            }
            return false;
        }

        public void UnregisterAllHotkeys()
        {
            foreach (int hotkeyId in _hotkeyConfigs.Keys)
            {
                UnregisterHotKey(_windowHandle, hotkeyId);
            }
            _hotkeyConfigs.Clear();
        }

        public HotkeyConfig GetConfig(int hotkeyId)
        {
            return _hotkeyConfigs.TryGetValue(hotkeyId, out HotkeyConfig config) ? config : null;
        }

        public void Clear()
        {
            UnregisterAllHotkeys();
            _currentHotkeyId = 1;
        }
    }
} 