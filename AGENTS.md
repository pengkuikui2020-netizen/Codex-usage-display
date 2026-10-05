# Codex Usage Display

Windows WPF 悬浮窗，显示当前 ChatGPT 账户的 Codex 5 小时与每周剩余额度。项目现状和验证记录见 `ROADMAP.md`，使用方式见 `README.md`。

## 目录和边界

- `src/CodexUsageWidget/`：窗口、WebView2 读取服务、额度解析与本地设置。
- `tests/`：无需真实登录的解析与刷新回归验证。
- `publish/`：本地构建交付物，不纳入 Git。
- `.tmp/<task-slug>/`：任务诊断与临时构建，不纳入 Git。

额度以剩余百分比展示；已用比例必须先换算。两个时间窗口必须独立读取，不能借用其他卡片的百分比。读取失败不得显示成功更新时间。WebView2 登录配置、Cookie、Token 和个人账户信息不得进入仓库、测试样本或对外日志。

## 验证入口

使用 .NET 8 SDK。编译入口为 `src/CodexUsageWidget/CodexUsageWidget.csproj`。修改读取或解析逻辑时执行 `tests/CodexUsageWidget.Tests` 的回归验证，并在本机已有登录状态下验证刷新；不能验证的部分记入 `ROADMAP.md`。
