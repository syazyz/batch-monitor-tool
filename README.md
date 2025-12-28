# Batch Monitor Tools

轻量级 WPF 工具，用于运行与监控多个批处理任务，支持实时输出、托盘运行及可配置启动行为。  
A lightweight WPF utility to run and monitor multiple batch files, with live output, tray support, and configurable startup behavior.

## Requirements / 运行要求
- Windows 10/11
- .NET 8 SDK (for building/running from source) / .NET 8 SDK（用于源码编译运行）

## Features / 功能概述
- Run multiple batch tasks with live stdout/stderr output / 多任务运行与实时输出
- Start/stop individual tasks, or start/stop all tasks / 单个或批量启动/停止
- Max output lines and clear output per task / 输出行数限制与清空
- Minimize to tray and optional start minimized / 最小化到托盘与启动最小化
- Optional run at Windows startup (HKCU Run key) / 可选开机自启（HKCU Run）
- Settings UI for editing task configuration / UI化配置任务

## Screenshots / 截图
![Monitor](assets/monitor.png)
![Settings](assets/settings.png)

## Getting started / 快速开始
1) Copy the example config and edit it / 复制示例配置并修改：
```json
// from repo root
// copy BatchMonitorTools/config.example.json to BatchMonitorTools/config.json
```
2) Update each task path/args in `BatchMonitorTools/config.json` / 更新任务路径与参数  
3) Run / 运行：
```bash
cd BatchMonitorTools
dotnet run
```

## Configuration / 配置说明
`BatchMonitorTools/config.json` controls tasks and app-level settings / 用于任务与应用设置：
- `tasks`: list of batch jobs to run / 任务列表
- `startMinimizedToTray`: start hidden and show tray icon / 启动最小化到托盘
- `runAtWindowsStartup`: register in HKCU Run key / 注册开机自启
- `autoScrollOutput`: auto-scroll output on updates / 输出自动滚动

## Notes / 备注
- If you enable "Run At Windows Startup", the app registers itself under
  `HKCU\Software\Microsoft\Windows\CurrentVersion\Run`.  
  启用开机自启后会写入 `HKCU\Software\Microsoft\Windows\CurrentVersion\Run`。

荣耀归于CODEX！
