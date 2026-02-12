# Session History

## 2025-12-28 10:01:29 UTC (v1.0.0)
- Planned WPF architecture for monitoring batch files, including MVVM structure, task runner, config flow, and UI layout.
- Established core components: config models, services for process execution/output capture, and per-task tabs with output views.
- Produced a baseline design that later became the initial implementation.

## 2025-12-28 11:11:32 UTC (v1.0.1)
- Updated task stop behavior to send CTRL+C first and only kill after a short timeout.
- Added a rule in AGENTS.md allowing comment-only edits without confirmation.
- Updated .gitignore to ignore AGENTS.md and pushed the change.

## 2025-12-30 21:21:30 UTC (v1.0.2)
- Reworked output rendering: buffered/batched updates, virtualized output list, and conditional auto-scroll.
- Fixed build issues and validated compilation.
- Built and published v1.0.2 release assets, added bilingual release notes and .NET runtime link.
- Added docs/architecture.md and ignored publish/ artifacts.

## 2026-01-10 08:02:00 UTC (v1.0.3)
- Added output search/filter UI with shared input, navigation buttons, and highlight toggle.
- Implemented output line matching, filtering, and match navigation in the task view model.
- Added keyword substring highlighting for output lines and updated log selection support.

## 2026-01-25 10:29:52 UTC (v1.0.4)
- Enabled Ctrl/Shift multi-select for output lines while keeping single-line text selection.
- Added context menu and Ctrl+C support for copying selected output lines in view order.

## 2026-02-12 00:00:00 UTC (v1.0.5)
- Improved stop behavior to gracefully terminate batch jobs by sending Ctrl+C first.
- Added automatic confirmation for `Terminate batch job (Y/N)?` prompts during stop.
- Increased graceful-stop timeout to 5 seconds before kill fallback.
