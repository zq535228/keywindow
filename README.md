# 快捷键窗体切换工具

一个用于 Windows 系统的快捷键窗口管理工具,可以通过自定义快捷键快速切换、启动和定位应用程序窗口。

## 主要功能

- 自定义全局快捷键绑定应用程序
- 支持快捷键映射功能
- 一键启动所有配置的应用程序
- 支持开机自启动
- 支持最小化到系统托盘
- 支持拖拽排序快捷键配置
- 自动保存配置到本地文件

## 技术特性

- 基于 .NET 9.0 开发
- 使用 Windows Forms 构建界面
- 使用 Win32 API 实现全局快捷键注册
- 使用 JSON 序列化保存配置
- 支持从进程和可执行文件中提取应用图标

![1737779150733.png](https://image.jianyandashu.com/i/2025/01/25/679467c75e15c.png)

![1737779746713.png](https://image.jianyandashu.com/i/2025/01/25/67946a1b401e2.png)

## 使用说明

1. 点击"添加快捷键"按钮添加新的快捷键配置
2. 在弹出的配置窗口中设置:
   - 快捷键组合(支持 Ctrl、Alt、Shift、Win)
   - 目标应用程序(可选择运行中的进程或指定可执行文件路径)
   - 可选的快捷键映射
3. 配置完成后点击确定保存
4. 使用设置的快捷键组合即可切换到目标应用程序窗口
5. 可以通过"一键启动"按钮同时启动所有配置的应用程序

## 系统要求

- Windows 操作系统
- .NET 9.0 运行时

## 开发环境

- Visual Studio 2022
- .NET 9.0 SDK
- Windows Forms

## 许可证

MIT

## 作者

检验大叔