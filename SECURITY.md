# Security

## Sensitive local data

Codex Usage Display uses a local Microsoft Edge WebView2 profile for ChatGPT authentication. The profile can contain session cookies and browser storage.

Never publish or attach these local folders or files:

- `%LOCALAPPDATA%\CodexUsageWidget\WebView2`
- `%APPDATA%\CodexUsageWidget\settings.json`
- Any copied WebView2 profile, cookie database, browser storage, session dump, `.env`, or secrets file

These files are not required to build the application.

## Reporting a security issue

If you find a security issue, avoid posting session data or credentials in a public issue. Provide only the minimum reproduction details necessary and redact tokens, cookies, account identifiers, and personal information.
