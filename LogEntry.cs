namespace LogViewer;

internal sealed class LogEntry
{
    public string Source { get; init; } = "";
    public string Level { get; init; } = "";
    public string Timestamp { get; init; } = "";
    public string MainMessage { get; set; } = "";
    public string FullText { get; set; } = "";

    public override string ToString()
    {
        string time = Timestamp.Length >= 19 ? Timestamp.Substring(11, 8) : Timestamp;
        string prefix = string.IsNullOrWhiteSpace(Level) ? "" : $"[{Level}] ";
        return $"{time}  {prefix}{MainMessage}";
    }
}
