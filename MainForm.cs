using Timer = System.Windows.Forms.Timer;

namespace Esma;

public sealed class MainForm : Form
{
    private const string TitleDefault = "ЕСМА";
    private const string TitleCopied = "✓ Скопировано";
    private const string TitleRefreshed = "✓ Обновлено";
    private const string RefreshItemLabel = "⟳ Обновить";

    private readonly AppConfig _config;
    private readonly ComboBox _combo;
    private readonly Timer _feedbackTimer;
    private readonly Timer _refreshTimer;
    private readonly Timer _topmostTimer;

    private List<(string Name, string Text)> _data = new();
    private DateTime? _lastWriteTime;

    public MainForm(AppConfig config)
    {
        _config = config;

        Text = TitleDefault;
        ClientSize = new Size(160, 50);
        FormBorderStyle = FormBorderStyle.FixedToolWindow;
        MaximizeBox = false;
        MinimizeBox = false;
        TopMost = true;
        StartPosition = FormStartPosition.WindowsDefaultLocation;

        _combo = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            Location = new Point(10, 14),
            Width = 140
        };
        _combo.SelectionChangeCommitted += OnSelect;
        Controls.Add(_combo);

        // Возврат заголовка после временной подсказки
        _feedbackTimer = new Timer { Interval = _config.FeedbackDurationMs };
        _feedbackTimer.Tick += (_, _) =>
        {
            _feedbackTimer.Stop();
            Text = TitleDefault;
        };

        // Периодическая проверка изменения файла
        _refreshTimer = new Timer { Interval = _config.RefreshCheckIntervalMs };
        _refreshTimer.Tick += (_, _) => CheckFileChanged();

        // Принудительное восстановление topmost (пока список не раскрыт)
        _topmostTimer = new Timer { Interval = _config.TopmostEnforceIntervalMs };
        _topmostTimer.Tick += (_, _) =>
        {
            if (!_combo.DroppedDown)
            {
                TopMost = false;
                TopMost = true;
            }
        };

        // Первичная загрузка; при ошибке список будет пуст,
        // но пункт «Обновить» останется доступен для повторной попытки
        LoadData(showErrors: true);
        RefreshComboValues();

        _refreshTimer.Start();
        _topmostTimer.Start();
    }

    // ───────────────────────── Загрузка данных ─────────────────────────

    private bool LoadData(bool showErrors)
    {
        try
        {
            var writeTime = File.GetLastWriteTimeUtc(_config.FilePath);
            var newData = ExcelReader.Read(_config.FilePath, _config.ExcelPassword);

            _data = newData;
            _lastWriteTime = writeTime;
            return true;
        }
        catch (FileNotFoundException)
        {
            if (showErrors)
                ShowError($"Файл не найден:\n{_config.FilePath}");
            return false;
        }
        catch (Exception ex)
        {
            if (showErrors)
                ShowError($"Не удалось прочитать файл {_config.FilePath}:\n{ex.Message}");
            return false;
        }
    }

    private void RefreshComboValues()
    {
        _combo.BeginUpdate();
        _combo.Items.Clear();
        foreach (var (name, _) in _data)
            _combo.Items.Add(name);
        _combo.Items.Add(RefreshItemLabel);
        _combo.EndUpdate();
    }

    private void CheckFileChanged()
    {
        try
        {
            var current = File.GetLastWriteTimeUtc(_config.FilePath);
            if (_lastWriteTime == null || current != _lastWriteTime)
            {
                if (LoadData(showErrors: false))
                    RefreshComboValues();
            }
        }
        catch
        {
            // Файл временно недоступен — повторим при следующей проверке
        }
    }

    // ───────────────────────── Обработка выбора ─────────────────────────

    private void OnSelect(object? sender, EventArgs e)
    {
        var selected = _combo.SelectedItem as string;
        if (selected == null) return;

        if (selected == RefreshItemLabel)
        {
            _combo.SelectedIndex = -1;
            if (LoadData(showErrors: true))
            {
                RefreshComboValues();
                FlashTitle(TitleRefreshed);
            }
            return;
        }

        var entry = _data.FirstOrDefault(d => d.Name == selected);
        if (entry.Name == null)
        {
            MessageBox.Show("Текстовое значение для выбранного имени не найдено.",
                TitleDefault, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        try
        {
            Clipboard.SetText(entry.Text);
            FlashTitle(TitleCopied);
        }
        catch (Exception ex)
        {
            ShowError($"Не удалось скопировать в буфер обмена:\n{ex.Message}");
        }
    }

    // ───────────────────────── Вспомогательное ─────────────────────────

    private void FlashTitle(string title)
    {
        Text = title;
        _feedbackTimer.Stop();
        _feedbackTimer.Start();
    }

    private static void ShowError(string message) =>
        MessageBox.Show(message, "ЕСМА — Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
}
