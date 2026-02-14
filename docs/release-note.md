# 发布与提交流程（Batch Monitor Tools）

本文记录本仓库当前采用的版本提交与 GitHub Release 流程。  
注意：网络代理配置不属于发布流程，不在本文范围内。

## 1. 版本变更落地

1. 修改代码并完成本地验证（至少 `dotnet build`）。
2. 更新版本文档：
   - `README.md` 中版本号（`Version: vX.Y.Z / 版本：vX.Y.Z`）
   - `docs/history.md` 新增对应版本条目
   - `docs/release-notes-vX.Y.Z.md` 新增本次发布说明（中英双语）

## 2. 发布产物构建方式

在仓库根目录执行：

```powershell
dotnet publish BatchMonitorTools/BatchMonitorTools.csproj -c Release -r win-x64 --self-contained false -o publish/framework-dependent -v minimal
dotnet publish BatchMonitorTools/BatchMonitorTools.csproj -c Release -r win-x64 --self-contained true -o publish/self-contained -v minimal
```

说明：
- `framework-dependent`：依赖目标机器安装 .NET 运行时。
- `self-contained`：包含运行时，可直接运行。

## 3. 打包命名规范

在 `publish` 目录根下输出两个 zip，命名固定为：

- `BatchMonitorTools-vX.Y.Z-framework-dependent-win-x64.zip`
- `BatchMonitorTools-vX.Y.Z-self-contained-win-x64.zip`

示例（v1.0.5）：

- `BatchMonitorTools-v1.0.5-framework-dependent-win-x64.zip`
- `BatchMonitorTools-v1.0.5-self-contained-win-x64.zip`

## 4. Release Notes 格式规范

`docs/release-notes-vX.Y.Z.md` 使用与现有版本一致的结构：

```md
# Release Notes - vX.Y.Z

## Highlights
- ...

## Details
- ...

## Notes
- ...

---

# 发布说明 - vX.Y.Z

## 亮点
- ...

## 细节
- ...

## 备注
- ...
```

## 5. Git 提交流程

建议拆成两次提交：

1. 功能提交（代码 + README/history）
2. 发布说明提交（`docs/release-notes-vX.Y.Z.md`）

示例：

```powershell
git add BatchMonitorTools/Services/BatchTaskRunner.cs README.md docs/history.md
git commit -m "release: vX.Y.Z <summary>"

git add docs/release-notes-vX.Y.Z.md
git commit -m "docs: add vX.Y.Z release notes"
```

## 6. 推送与发布方式（GitHub + gh）

1. 推送主分支：

```powershell
git push -u origin main
```

2. 创建 Release 并上传两个 zip：

```powershell
gh release create vX.Y.Z `
  "publish/BatchMonitorTools-vX.Y.Z-framework-dependent-win-x64.zip" `
  "publish/BatchMonitorTools-vX.Y.Z-self-contained-win-x64.zip" `
  --repo syazyz/batch-monitor-tool `
  --title "vX.Y.Z" `
  --notes-file "docs/release-notes-vX.Y.Z.md" `
  --target main
```

3. 验证 Release 资产：

```powershell
gh release view vX.Y.Z --repo syazyz/batch-monitor-tool --json url,tagName,name,assets
```

### 6.1 常见问题：`gh auth` 状态与本地终端不一致

现象：
- 在自动化/受限执行环境中，`gh auth status` 报 token 无效，或 `git push` 报 `could not read Username`。
- 但在你本地外部终端中，`gh auth status` 显示已登录且可用。

原因：
- 命令执行环境与本地交互终端不在同一凭据上下文，可能无法读取 keyring、`~/.gitconfig` 或 credential helper。

处理步骤：

1. 在当前发布执行环境先检查：

```powershell
gh auth status
git remote -v
```

2. 若 `gh` 异常，先修复认证：

```powershell
gh auth login -h github.com
gh auth setup-git
```

3. 确保远端使用 HTTPS（与 `gh` 认证协议一致）：

```powershell
git remote set-url origin https://github.com/syazyz/batch-monitor-tool.git
```

4. 重新执行：

```powershell
git push -u origin main
gh release create vX.Y.Z ...
```

5. 若外部终端可用但自动化环境仍失败：
- 在与外部终端相同的权限上下文执行发布命令（确保能访问同一 keyring/credential helper）。

## 7. 发布检查清单

- `README.md` 版本号已更新
- `docs/history.md` 已追加版本记录
- `docs/release-notes-vX.Y.Z.md` 已创建并符合格式
- `publish` 根目录存在两个命名正确的 zip
- `git status` 干净
- `origin/main` 已推送
- GitHub Release 页面可访问，资产齐全
