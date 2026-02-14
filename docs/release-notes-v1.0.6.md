# Release Notes - v1.0.6

## Highlights
- Added a per-task `InputRedirect` switch in the Settings grid.
- Improved stop safety for tasks that run with input redirection disabled.

## Details
- Added task config field `enableInputRedirect` (default `true`) and UI binding.
- Task runner now applies `RedirectStandardInput` based on each task's `enableInputRedirect`.
- When input redirection is disabled, stop skips Ctrl+C signaling and uses process-tree kill fallback directly.

## Notes
- Existing configs remain compatible; missing `enableInputRedirect` defaults to `true`.
- For tools that report `Input redirection is not supported`, set `InputRedirect` to off for that task.

---

# 发布说明 - v1.0.6

## 亮点
- 设置表格新增任务级 `InputRedirect` 开关。
- 优化了关闭输入重定向任务时的停止安全性。

## 细节
- 新增任务配置字段 `enableInputRedirect`（默认 `true`）并完成界面绑定。
- 任务启动时按任务配置决定是否启用 `RedirectStandardInput`。
- 当输入重定向关闭时，停止流程跳过 Ctrl+C 信号，直接使用进程树强制结束兜底。

## 备注
- 旧配置保持兼容；未包含 `enableInputRedirect` 时默认按 `true` 处理。
- 对报错 `Input redirection is not supported` 的程序，可在该任务上关闭 `InputRedirect`。
