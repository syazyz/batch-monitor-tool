# Architecture Overview (v1.0.0 Baseline)

This note summarizes the core source files and their responsibilities.
Build outputs (bin/obj) are intentionally omitted.

## App Entry & Project
- BatchMonitorTools/BatchMonitorTools.csproj: WPF project definition (target framework, WPF/WinForms usage, config copy rules).
- BatchMonitorTools/App.xaml: Application resources and startup entry.
- BatchMonitorTools/App.xaml.cs: Application bootstrap; delegates to the main window.
- BatchMonitorTools/AssemblyInfo.cs: Assembly metadata.

## UI (Window)
- BatchMonitorTools/MainWindow.xaml: Main UI layout (tabs, task controls, output view, settings).
- BatchMonitorTools/MainWindow.xaml: Adds output query controls (mode, input, navigation, highlight toggle) and renders highlighted output text.
- BatchMonitorTools/MainWindow.xaml.cs: Window behavior (tray icon, minimize/restore, auto-scroll handling).

## ViewModels (MVVM State & Commands)
- BatchMonitorTools/ViewModels/MainViewModel.cs: App-level state and commands; loads/saves config, manages task list, handles start/stop all, startup settings, auto-scroll.
- BatchMonitorTools/ViewModels/BatchTaskViewModel.cs: Per-task state and commands; start/stop, output buffering/display, line limits, status text.
- BatchMonitorTools/ViewModels/BatchTaskViewModel.cs: Adds output query state, filtering, match navigation, and highlight control for per-task output.

## Services (Runtime Logic)
- BatchMonitorTools/Services/ITaskRunner.cs: Abstraction for running/stopping tasks with output events.
- BatchMonitorTools/Services/BatchTaskRunner.cs: Real process runner; starts .bat via Process, captures stdout/stderr, stop/exit flow.
- BatchMonitorTools/Services/FakeTaskRunner.cs: Test stub that emits periodic output.
- BatchMonitorTools/Services/ConfigService.cs: Load/save config.json alongside the executable.

## Config Models
- BatchMonitorTools/Config/AppConfig.cs: Root settings (task list, startup flags, auto-scroll).
- BatchMonitorTools/Config/BatchTaskConfig.cs: Per-task config (name, path, args, auto-start, max output lines).

## Command Helper
- BatchMonitorTools/Commands/RelayCommand.cs: ICommand implementation for binding UI actions.

## Helpers
- BatchMonitorTools/Helpers/TextHighlighting.cs: Builds inline runs to highlight keyword substrings in output lines.

## Config Files
- BatchMonitorTools/config.json: Runtime config (user data, kept next to the executable).
- BatchMonitorTools/config.example.json: Example template for new installs.
