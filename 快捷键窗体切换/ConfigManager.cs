using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Windows.Forms;

namespace 快捷键窗体切换
{
    public class ConfigManager
    {
        private const string CONFIG_FILE = "hotkeys.json";

        public void SaveConfig(Dictionary<int, HotkeyConfig> hotkeyConfigs, ListView listView)
        {
            try
            {
                var configs = new List<SavedHotkeyConfig>();
                
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

        public List<SavedHotkeyConfig> LoadConfig()
        {
            try
            {
                if (File.Exists(CONFIG_FILE))
                {
                    string jsonString = File.ReadAllText(CONFIG_FILE);
                    return JsonSerializer.Deserialize<List<SavedHotkeyConfig>>(jsonString);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"加载配置失败: {ex.Message}");
            }
            return new List<SavedHotkeyConfig>();
        }
    }
} 