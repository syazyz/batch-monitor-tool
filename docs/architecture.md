# Architecture Overview (v1.0.6)

This note summarizes the core source files and their responsibilities.
Build outputs (bin/obj) are intentionally omitted.

## App Entry & Project
- src/BatchMonitorTools.csproj: WPF project definition (target framework, WPF/WinForms usage, config copy rules).
- src/App.xaml: Application resources and startup entry.
- src/App.xaml.cs: Application bootstrap; delegates to the main window.
- src/AssemblyInfo.cs: Assembly metadata.

## UI (Window)
- src/MainWindow.xaml: Main UI layout (tabs, task controls, output view, settings).
- src/MainWindow.xaml: Adds output query controls (mode, input, navigation, highlight toggle) and renders highlighted output text.
- src/MainWindow.xaml: Enables multi-select output copy with Ctrl/Shift selection and a context menu for copying lines.
- src/MainWindow.xaml.cs: Window behavior (tray icon, minimize/restore, auto-scroll handling, multi-select output copy).

## ViewModels (MVVM State & Commands)
- src/ViewModels/MainViewModel.cs: App-level state and commands; loads/saves config, manages task list, handles start/stop all, startup settings, auto-scroll.
- src/ViewModels/BatchTaskViewModel.cs: Per-task state and commands; start/stop, output buffering/display, line limits, status text.
- src/ViewModels/BatchTaskViewModel.cs: Adds output query state, filtering, match navigation, and highlight control for per-task output.

## Services (Runtime Logic)
- src/Services/ITaskRunner.cs: Abstraction for running/stopping tasks with output events.
- src/Services/BatchTaskRunner.cs: Real process runner; starts .bat via Process, captures stdout/stderr, stop/exit flow.
- src/Services/FakeTaskRunner.cs: Test stub that emits periodic output.
- src/Services/ConfigService.cs: Load/save config.json alongside the executable.

## Config Models
- src/Config/AppConfig.cs: Root settings (task list, startup flags, auto-scroll).
- src/Config/BatchTaskConfig.cs: Per-task config (name, path, args, auto-start, max output lines).

## Command Helper
- src/Commands/RelayCommand.cs: ICommand implementation for binding UI actions.

## Helpers
- src/Helpers/TextHighlighting.cs: Builds inline runs to highlight keyword substrings in output lines.

## Config Files
- src/config.json: Runtime config (user data, kept next to the executable).
- src/config.example.json: Example template for new installs.
