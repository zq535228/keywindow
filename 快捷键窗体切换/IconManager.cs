using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace 快捷键窗体切换
{
    public class IconManager
    {
        [DllImport("shell32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr ExtractIcon(IntPtr hInst, string lpszExeFileName, int nIconIndex);

        public Icon GetFileIcon(string path)
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

        public string GetIconPath(string processName, string appPath)
        {
            if (!string.IsNullOrEmpty(appPath))
                return appPath;

            var process = Process.GetProcessesByName(processName).FirstOrDefault();
            if (process != null && !string.IsNullOrEmpty(process.MainModule?.FileName))
            {
                return process.MainModule.FileName;
            }

            return null;
        }
    }
} 