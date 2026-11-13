using System.Text;
using System.Text.RegularExpressions;
using System.Linq;

namespace LogViewer;

internal sealed class MainForm : Form
{
    private readonly Button openButton = new();
    private readonly ComboBox sourceCombo = new();
    private readonly TextBox searchBox = new();
    private readonly ListBox messageList = new();
    private readonly RichTextBox detailsBox = new();
    private readonly Label statusLabel = new();
    private List<LogEntry> allEntries = new();
    private List<LogEntry> visibleEntries = new();

    public MainForm()
    {
        Text = "Log Viewer";
        Width = 1250;
        Height = 820;
        MinimumSize = new Size(850, 600);
        StartPosition = FormStartPosition.CenterScreen;
        Font = new Font("Segoe UI", 9F);

        BuildLayout();
        UpdateStatus("Откройте .log или .txt файл");
    }

    private void BuildLayout()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 4,
            Padding = new Padding(10)
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 66));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 55));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 45));
        Controls.Add(root);

        var top = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false
        };
        openButton.Text = "Открыть лог...";
        openButton.Width = 130;
        openButton.Height = 30;
        openButton.Click += (_, _) => OpenLog();
        top.Controls.Add(openButton);

        statusLabel.AutoSize = false;
        statusLabel.Width = 850;
        statusLabel.Height = 30;
        statusLabel.TextAlign = ContentAlignment.MiddleLeft;
        statusLabel.Padding = new Padding(10, 0, 0, 0);
        top.Controls.Add(statusLabel);
        root.Controls.Add(top, 0, 0);

        var filters = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 2
        };
        filters.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 62));
        filters.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 38));
        filters.RowStyles.Add(new RowStyle(SizeType.Absolute, 20));
        filters.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        filters.Controls.Add(new Label { Text = "Источник лога", Dock = DockStyle.Fill }, 0, 0);
        filters.Controls.Add(new Label { Text = "Поиск по сообщениям", Dock = DockStyle.Fill }, 1, 0);

        sourceCombo.Dock = DockStyle.Fill;
        sourceCombo.DropDownStyle = ComboBoxStyle.DropDownList;
        sourceCombo.SelectedIndexChanged += (_, _) => RefreshMessageList();
        filters.Controls.Add(sourceCombo, 0, 1);

        searchBox.Dock = DockStyle.Fill;
        searchBox.PlaceholderText = "Текст для поиска...";
        searchBox.TextChanged += (_, _) => RefreshMessageList();
        filters.Controls.Add(searchBox, 1, 1);
        root.Controls.Add(filters, 0, 1);

        var split = new SplitContainer
        {
            Dock = DockStyle.Fill,
            Orientation = Orientation.Horizontal,
            SplitterDistance = 280,
            Panel1MinSize = 100,
            Panel2MinSize = 100
        };

        messageList.Dock = DockStyle.Fill;
        messageList.HorizontalScrollbar = true;
        messageList.IntegralHeight = false;
        messageList.Font = new Font("Consolas", 9F);
        messageList.SelectedIndexChanged += (_, _) => ShowSelectedEntry();
        split.Panel1.Controls.Add(messageList);

        detailsBox.Dock = DockStyle.Fill;
        detailsBox.ReadOnly = true;
        detailsBox.WordWrap = false;
        detailsBox.Font = new Font("Consolas", 9F);
        detailsBox.BackColor = Color.FromArgb(250, 250, 250);
        split.Panel2.Controls.Add(detailsBox);

        root.Controls.Add(split, 0, 2);
        root.SetRowSpan(split, 2);
    }


    private void OpenLog()
    {
        using var dialog = new OpenFileDialog
        {
            Title = "Выберите лог-файл",
            Filter = "Log files (*.log;*.txt)|*.log;*.txt|All files (*.*)|*.*",
            CheckFileExists = true
        };

        if (dialog.ShowDialog(this) != DialogResult.OK) return;

        try
        {
            string content = File.ReadAllText(dialog.FileName, Encoding.UTF8);
            allEntries = LogParser.Parse(content);
            Text = $"Log Viewer — {Path.GetFileName(dialog.FileName)}";
            PopulateSources();
            UpdateStatus($"Загружено событий: {allEntries.Count:N0}");
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"Не удалось прочитать файл:\n{ex.Message}",
                "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void PopulateSources()
    {
        string? previous = sourceCombo.SelectedItem?.ToString();
        var sources = allEntries
            .Select(x => x.Source)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(x => x == "Java Errors" ? 0 : 1)
            .ThenBy(x => x, StringComparer.Ordinal)
            .ToList();

        sourceCombo.BeginUpdate();
        sourceCombo.Items.Clear();
        sourceCombo.Items.Add("Все источники");

        var displaySources = sources
            .Select(s => Regex.Replace(s, @"^(?:\.?[a-zA-Z]\.)+", ""))
            .Distinct()
            .OrderBy(s => s, StringComparer.OrdinalIgnoreCase);

        if (displaySources.Contains("Java Errors"))
            sourceCombo.Items.Add("Java Errors");;

        foreach (string source in displaySources.Where(s => s != "Java Errors"))
        {
            sourceCombo.Items.Add(source);
        }

        sourceCombo.SelectedIndex = 0;

        int index = previous is null ? 0 : sourceCombo.Items.IndexOf(previous);
        sourceCombo.SelectedIndex = index >= 0 ? index : 0;
        sourceCombo.EndUpdate();
        RefreshMessageList();
    }

    private void RefreshMessageList()
    {
        if (sourceCombo.SelectedItem is null) return;

        string selectedSource = sourceCombo.SelectedItem.ToString()!;
        string search = searchBox.Text.Trim();

        IEnumerable<LogEntry> query = allEntries;
        if (selectedSource != "Все источники")
            query = query.Where(x =>
                Regex.Replace(x.Source, @"^(?:\.?[a-zA-Z]\.)+", "") == selectedSource);

        if (search.Length > 0)
            query = query.Where(x =>
                x.MainMessage.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                x.FullText.Contains(search, StringComparison.OrdinalIgnoreCase));

        visibleEntries = query.ToList();

        messageList.BeginUpdate();
        messageList.Items.Clear();
        foreach (LogEntry entry in visibleEntries)
            messageList.Items.Add(entry);
        messageList.EndUpdate();

        detailsBox.Clear();
        UpdateStatus($"Событий: {visibleEntries.Count:N0} из {allEntries.Count:N0}");
    }

    private void ShowSelectedEntry()
    {
        int index = messageList.SelectedIndex;
        if (index < 0 || index >= visibleEntries.Count) return;

        LogEntry entry = visibleEntries[index];
        detailsBox.Text = entry.FullText;
        detailsBox.SelectionStart = 0;
        detailsBox.SelectionLength = 0;
    }

    private void UpdateStatus(string text) => statusLabel.Text = text;
}
