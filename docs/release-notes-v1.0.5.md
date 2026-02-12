# Release Notes - v1.0.5

## Highlights
- Improved stop behavior to gracefully terminate batch jobs via Ctrl+C.
- Added automatic confirmation for `Terminate batch job (Y/N)?` prompts.

## Details
- Stop now redirects stdin and sends `Y` after Ctrl+C to confirm batch termination.
- Graceful-stop timeout is increased to 5 seconds before force-kill fallback.

## Notes
- No configuration changes required.
- Existing start/stop UI operations are unchanged.

---

# 发布说明 - v1.0.5

## 亮点
- 优化停止流程：优先通过 Ctrl+C 优雅终止批处理任务。
- 新增对 `Terminate batch job (Y/N)?` 提示的自动确认。

## 细节
- 停止时会重定向标准输入，并在 Ctrl+C 后自动发送 `Y` 进行确认。
- 优雅停止等待时间提升到 5 秒，超时后再走强制结束兜底。

## 备注
- 无需修改配置。
- 现有启动/停止界面操作方式不变。
