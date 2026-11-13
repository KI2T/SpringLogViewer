
using System.Text;
using System.Text.RegularExpressions;

namespace LogViewer;

internal static class LogParser
{
    // Spring Boot pattern:
    // 2026-11-12T11:58:09.482Z INFO 7 --- [thread] logger : message
    private static readonly Regex LogLineRegex = new(
        @"^(?<timestamp>\d{4}-\d{2}-\d{2}[T ][0-9:.]+Z?)\s+(?<level>TRACE|DEBUG|INFO|WARN|ERROR)\s+\S+\s+---\s+\[(?<thread>.*?)\]\s+(?<logger>.*?)\s+:\s?(?<message>.*)$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex ExceptionStartRegex = new(
        @"^(?:(?:Exception in thread\s+""[^""]+""\s+)|(?:Caused by:\s+)|(?:Suppressed:\s+))?(?:[\w$]+\.)*[\w$]*(?:Exception|Error|Throwable)\s*(?::.*)?$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex StackStartRegex = new(
        @"^(?:\s+at\s+|Caused by:\s+|Suppressed:\s+|\.\.\.\s+\d+\s+(?:common frames omitted|more)\s*)",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public static List<LogEntry> Parse(string text)
    {
        var result = new List<LogEntry>();

        LogEntry? current = null;
        var currentText = new StringBuilder();
        bool currentIsJavaError = false;

        // Последние 5 физических строк исходного лога
        var recentLines = new Queue<string>();

        void Flush()
        {
            if (current is null)
                return;

            current.FullText = currentText.ToString().TrimEnd();

            if (string.IsNullOrWhiteSpace(current.MainMessage))
            {
                current.MainMessage = currentIsJavaError
                    ? "Java exception"
                    : "(empty message)";
            }

            result.Add(current);

            current = null;
            currentText.Clear();
            currentIsJavaError = false;
        }

        foreach (string line in text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n'))
        {
            // Сохраняем предыдущие 5 строк ДО обработки текущей
            string[] previousLines = recentLines.ToArray();

            recentLines.Enqueue(line);

            if (recentLines.Count > 5)
                recentLines.Dequeue();

            Match match = LogLineRegex.Match(line);

            // Обычная строка лога
            if (match.Success)
            {
                Flush();

                string message = match.Groups["message"].Value;

                current = new LogEntry
                {
                    Source = match.Groups["logger"].Value,
                    Level = match.Groups["level"].Value,
                    Timestamp = match.Groups["timestamp"].Value,
                    MainMessage = message
                };

                currentText.AppendLine(line);
                continue;
            }

            // Обнаружено начало Java Exception / Error
            if (IsExceptionStart(line))
            {
                // Вложенные исключения принадлежат текущему stack trace
                if (currentIsJavaError && current is not null)
                {
                    currentText.AppendLine(line);
                    continue;
                }

                Flush();

                currentIsJavaError = true;

                string exceptionMessage = line.Trim();

                current = new LogEntry
                {
                    Source = "Java Errors",
                    Level = "ERROR",
                    Timestamp = "",
                    MainMessage = exceptionMessage
                };

                // Контекст: последние 5 строк перед исключением
                currentText.AppendLine(
                    "========== Последние строки лога перед ошибкой ==========");

                foreach (string previousLine in previousLines)
                    currentText.AppendLine(previousLine);

                currentText.AppendLine(
                    "========== STACK TRACE ==========");

                currentText.AppendLine(line);

                continue;
            }

            // Строка не распознана как обычная запись или начало Exception
            if (current is null)
            {
                if (!string.IsNullOrWhiteSpace(line))
                {
                    current = new LogEntry
                    {
                        Source = "Unparsed",
                        Level = "",
                        MainMessage = FirstUsefulLine(line)
                    };

                    currentText.AppendLine(line);
                }

                continue;
            }

            // Продолжение текущей записи или stack trace
            currentText.AppendLine(line);
        }

        Flush();

        return result;
    }

    private static bool IsExceptionStart(string line)
    {
        string trimmed = line.Trim();

        if (trimmed.Length == 0)
            return false;

        if (ExceptionStartRegex.IsMatch(trimmed))
            return true;

        // Обрабатываем распространённые имена исключений,
        // которые не заканчиваются строго на Exception.
        return Regex.IsMatch(
            trimmed,
            @"^(?:[\w$]+\.)+[\w$]*(?:Exception|Error|Throwable)\s*:",
            RegexOptions.CultureInvariant);
    }

    private static string FirstUsefulLine(string message)
    {
        string first = message.Split('\n').FirstOrDefault()?.Trim() ?? "";

        return first.Length > 180
            ? first[..177] + "..."
            : first;
    }
}
