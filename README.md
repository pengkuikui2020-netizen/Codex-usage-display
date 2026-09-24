# Codex Usage Display

A small Windows always-on-top widget that displays the remaining ChatGPT Codex usage limits from the Codex Analytics page.

> Unofficial community utility. Not affiliated with or endorsed by OpenAI.

## Features

- Shows the **5-hour usage limit** and **weekly usage limit**
- Shows remaining percentage and reset information
- Small borderless always-on-top window
- Draggable and remembers its last position
- Manual refresh and automatic refresh (default: every 2 minutes)
- Supports English and Chinese ChatGPT UI text
- Uses Microsoft Edge WebView2 for local sign-in
- Does **not** read Chrome cookies
- Does **not** store your ChatGPT password
- No third-party server; page parsing happens locally

## Privacy and security

The app uses a dedicated WebView2 profile stored locally at:

`%LOCALAPPDATA%\CodexUsageWidget\WebView2`

That folder can contain your local ChatGPT session cookies and **must never be uploaded or shared**.

Normal window settings are stored at:

`%APPDATA%\CodexUsageWidget\settings.json`

Neither location is inside the source repository. The included `.gitignore` also excludes common local profile, settings, build-output, log, and secret files.

The source code contains no API key or embedded ChatGPT credential. Authentication happens interactively in WebView2 on the user's own machine.

## Requirements

- Windows 10 or Windows 11
- .NET 8 SDK (for building)
- Microsoft Edge WebView2 Runtime (normally already present on modern Windows)

## Build

Double-click:

`BUILD.cmd`

Or run:

```powershell
dotnet restore ".\src\CodexUsageWidget\CodexUsageWidget.csproj"
dotnet publish ".\src\CodexUsageWidget\CodexUsageWidget.csproj" -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true -o ".\publish"
```

The executable will be created at:

`publish\CodexUsageWidget.exe`

## First run

1. Start `CodexUsageWidget.exe`.
2. If required, open the sign-in window from the widget.
3. Sign in to ChatGPT normally.
4. Return to the widget and refresh.

The app reads the visible text from:

`https://chatgpt.com/codex/cloud/settings/analytics`

Because ChatGPT is a client-rendered web application, the parser waits for the usage cards to load before extracting the percentages.

## How it works

The app intentionally avoids depending on fragile CSS class names. It reads visible page text and recognizes wording such as:

- `5 hour usage limit`
- `Weekly usage limit`
- `87% remaining`
- Chinese equivalents such as `剩余 87%`

If OpenAI changes the wording or page structure, the parser may need to be updated in:

`src/CodexUsageWidget/Services/CodexUsageParser.cs`

## Current scope

This version intentionally does not include:

- Startup-with-Windows
- System tray integration
- Low-quota notifications
- Reading another browser's login state
- Any remote backend or telemetry

## Security note for contributors

Do not commit or attach any WebView2 profile, cookies, browser storage, session export, `settings.json`, `.env` file, or local debug logs. Before publishing a fork, review `git status` and the staged diff.

## License

No license has been selected yet. Add the license you want before encouraging redistribution or modification.
