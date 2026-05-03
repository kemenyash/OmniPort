# OmniPort

OmniPort is a .NET 10 app for converting CSV, Excel, JSON, and XML data with reusable transformation templates.

It can process uploaded files, convert data from URLs, keep conversion history, and watch remote URLs on a schedule. Watched URLs expose a stable latest-result link that can be used by external services.

## Features

- Create source and target templates.
- Map fields between templates.
- Convert uploaded files or remote URLs.
- Keep file and URL conversion history.
- Watch URLs and automatically refresh converted output.
- Copy result links and stable watched links from the UI.
- View application logs in the `/logs` page.
- Check app health at `/health`.

## Supported Formats

| Format | Extensions |
| --- | --- |
| CSV | `.csv` |
| Excel | `.xls`, `.xlsx` |
| JSON | `.json` |
| XML | `.xml` |

## Stable Links

Watched URLs expose a permanent endpoint:

```text
/watch/{watchedUrlId}/latest
```

This URL redirects to the latest successful conversion result for that watched URL.

## Logs

Application logs are written to:

```text
src/Web/logs/omniport-app.jsonl
```

Use `/logs` in the app to view recent and older log entries or change the runtime log level.

## Run Locally

Requirements:

- .NET 10 SDK

Build:

```powershell
dotnet build OmniPort.sln
```

Run:

```powershell
dotnet run --project src\Web\Web.csproj
```

## Test

```powershell
dotnet test OmniPort.sln
```
