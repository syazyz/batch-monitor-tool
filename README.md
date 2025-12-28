# Batch Monitor Tools

A lightweight WPF utility to run and monitor multiple batch files, with live output, tray support, and configurable startup behavior.

## Requirements
- Windows 10/11
- .NET 8 SDK (for building/running from source)

## Features
- Run multiple batch tasks with live stdout/stderr output
- Start/stop individual tasks, or start/stop all tasks
- Max output lines and clear output per task
- Minimize to tray and optional start minimized
- Optional run at Windows startup (HKCU Run key)
- Settings UI for editing task configuration

## Getting started
1) Copy the example config and edit it:
```json
// from repo root
// copy BatchMonitorTools/config.example.json to BatchMonitorTools/config.json
```
2) Update each task path/args in `BatchMonitorTools/config.json`
3) Run:
```bash
cd BatchMonitorTools
dotnet run
```

## Configuration
`BatchMonitorTools/config.json` controls tasks and app-level settings:
- `tasks`: list of batch jobs to run
- `startMinimizedToTray`: start hidden and show tray icon
- `runAtWindowsStartup`: register in HKCU Run key
- `autoScrollOutput`: auto-scroll output on updates

## Notes
- If you enable "Run At Windows Startup", the app registers itself under
  `HKCU\Software\Microsoft\Windows\CurrentVersion\Run`.
