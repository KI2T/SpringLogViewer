# Log Viewer

Небольшой просмотрщик Spring Boot логов на C# WinForms (.NET 8).

## Возможности
- Открытие `.log` и `.txt`.
- Dropdown со всеми обнаруженными logger/source.
- Отдельный источник `Java Errors` для Java stack trace.
- Список событий с временем, уровнем и первой строкой сообщения.
- Полный текст выбранного события в нижней панели.
- Поиск по сообщению и полному тексту события.
- Многострочные продолжения сохраняются.

## Запуск
1. Установить .NET 8 SDK.
2. Открыть `LogViewer.csproj` в Visual Studio 2022 или Rider.
3. Собрать и запустить проект.

Из командной строки (Windows):
```powershell
dotnet build .\LogViewer\LogViewer.csproj
dotnet run --project .\LogViewer\LogViewer.csproj
```

Приложение рассчитано на Windows.
