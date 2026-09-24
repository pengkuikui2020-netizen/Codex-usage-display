# Codex Usage Display

A small Windows always-on-top widget that displays the remaining ChatGPT Codex usage limits from the Codex Analytics page.  
一个适用于 Windows 的小巧置顶悬浮窗，用于显示 ChatGPT Codex Analytics 页面中的 5 小时和每周剩余使用额度。

> Unofficial community utility. Not affiliated with or endorsed by OpenAI.  
> 非官方社区工具，与 OpenAI 无隶属关系，也不代表 OpenAI 官方认可。

![Codex Usage Display](docs/codex-usage-display.png)

## Features / 功能

- Shows the **5-hour usage limit** and **weekly usage limit**  
  显示 **5 小时使用限额** 和 **每周使用限额**
- Shows remaining percentage and reset information  
  显示剩余百分比和额度重置时间
- Small borderless always-on-top window  
  小巧、无边框、始终置顶的悬浮窗口
- Draggable and remembers its last position  
  支持拖动，并记住上次窗口位置
- Manual refresh and automatic refresh (default: every 2 minutes)  
  支持手动刷新和自动刷新（默认每 2 分钟一次）
- Supports English and Chinese ChatGPT UI text  
  支持识别英文和中文 ChatGPT 页面文字
- Uses Microsoft Edge WebView2 for local sign-in  
  使用 Microsoft Edge WebView2 在本机完成登录
- Does **not** read Chrome cookies  
  **不会**读取 Chrome Cookie
- Does **not** store your ChatGPT password  
  **不会**保存你的 ChatGPT 密码
- No third-party server; page parsing happens locally  
  不使用第三方服务器，页面解析全部在本机完成

## Privacy and security / 隐私与安全

The app uses a dedicated WebView2 profile stored locally at:  
程序使用独立的 WebView2 配置目录，本地保存于：

`%LOCALAPPDATA%\CodexUsageWidget\WebView2`

That folder can contain your local ChatGPT session cookies and **must never be uploaded or shared**.  
该目录可能包含你的 ChatGPT 登录会话 Cookie，**绝对不要上传或分享该目录**。

Normal window settings are stored at:  
普通窗口设置保存在：

`%APPDATA%\CodexUsageWidget\settings.json`

Neither location is inside the source repository. The included `.gitignore` also excludes common local profile, settings, build-output, log, and secret files.  
以上两个目录都不在源码仓库中；项目自带的 `.gitignore` 也会排除常见的本地配置、构建产物、日志和敏感文件。

The source code contains no API key or embedded ChatGPT credential. Authentication happens interactively in WebView2 on the user's own machine.  
源码中不包含 API Key，也没有写死任何 ChatGPT 凭据。登录由用户在自己电脑上的 WebView2 中交互完成。

## Requirements / 运行与构建要求

- Windows 10 or Windows 11  
  Windows 10 或 Windows 11
- .NET 8 SDK (for building)  
  .NET 8 SDK（用于编译）
- Microsoft Edge WebView2 Runtime (normally already present on modern Windows)  
  Microsoft Edge WebView2 Runtime（现代 Windows 通常已经自带）

## Build / 编译

Double-click / 双击：

`BUILD.cmd`

Or run / 或运行：

```powershell
dotnet restore ".\src\CodexUsageWidget\CodexUsageWidget.csproj"
dotnet publish ".\src\CodexUsageWidget\CodexUsageWidget.csproj" -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true -o ".\publish"
```

The executable will be created at:  
可执行文件将生成在：

`publish\CodexUsageWidget.exe`

## First run / 首次运行

1. Start `CodexUsageWidget.exe`.  
   启动 `CodexUsageWidget.exe`。
2. If required, open the sign-in window from the widget.  
   如果需要登录，从悬浮窗打开登录窗口。
3. Sign in to ChatGPT normally.  
   正常登录 ChatGPT。
4. Return to the widget and refresh.  
   回到悬浮窗并刷新。

The app reads the visible text from:  
程序读取以下页面中的可见文字：

`https://chatgpt.com/codex/cloud/settings/analytics`

Because ChatGPT is a client-rendered web application, the parser waits for the usage cards to load before extracting the percentages.  
由于 ChatGPT 页面采用前端动态加载，程序会等待额度卡片加载完成后再提取百分比。

## How it works / 工作原理

The app intentionally avoids depending on fragile CSS class names. It reads visible page text and recognizes wording such as:  
程序尽量避免依赖容易变化的 CSS 类名，而是读取页面可见文字，并识别类似以下内容：

- `5 hour usage limit`
- `Weekly usage limit`
- `87% remaining`
- Chinese equivalents such as `剩余 87%`  
  以及中文页面中的 `剩余 87%` 等对应文字

If OpenAI changes the wording or page structure, the parser may need to be updated in:  
如果 OpenAI 将来修改了页面文案或结构，可能需要更新：

`src/CodexUsageWidget/Services/CodexUsageParser.cs`

## Current scope / 当前版本范围

This version intentionally does not include:  
当前版本暂不包含：

- Startup-with-Windows / Windows 开机自启动
- System tray integration / 系统托盘集成
- Low-quota notifications / 低额度提醒
- Reading another browser's login state / 读取其他浏览器的登录状态
- Any remote backend or telemetry / 任何远程后端或遥测功能

## Security note for contributors / 给贡献者的安全提示

Do not commit or attach any WebView2 profile, cookies, browser storage, session export, `settings.json`, `.env` file, or local debug logs. Before publishing a fork, review `git status` and the staged diff.  
不要提交或上传任何 WebView2 配置目录、Cookie、浏览器存储、会话导出文件、`settings.json`、`.env` 或本地调试日志。发布 fork 前，请先检查 `git status` 和暂存区 diff。

## License / 许可证

No license has been selected yet. Add the license you want before encouraging redistribution or modification.  
目前尚未选择开源许可证。如果希望其他人合法修改、再发布或二次开发，建议后续添加合适的开源许可证。
