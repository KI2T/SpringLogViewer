# Log Viewer

A lightweight Spring Boot log viewer built with C# WinForms (.NET 8).

## Features

- Open `.log` and `.txt` files.
- Dropdown list containing all detected loggers/sources.
- Dedicated `Java Errors` source for Java stack traces.
- Event list displaying timestamps, log levels, and the first line of each message.
- Full text of the selected event displayed in the bottom panel.
- Search by message content and full event text.
- Multiline log continuations are preserved.

## Getting Started

1. Install the .NET 8 SDK.
2. Open `LogViewer.csproj` in Visual Studio 2022 or Rider.
3. Build and run the project.

From the command line (Windows):

```powershell
dotnet build .\LogViewer\LogViewer.csproj
dotnet run --project .\LogViewer\LogViewer.csproj
```

The application is designed for Windows.
