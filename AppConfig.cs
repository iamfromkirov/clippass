using System.Text.Json;

namespace Esma;

/// <summary>Настройки приложения. Читаются из config.json рядом с exe.</summary>
public sealed class AppConfig
{
    public string FilePath { get; set; } = "data.xlsx";
    public string ExcelPassword { get; set; } = "";
    public int FeedbackDurationMs { get; set; } = 800;
    public int RefreshCheckIntervalMs { get; set; } = 30 * 60 * 1000;
    public int TopmostEnforceIntervalMs { get; set; } = 10 * 1000;

    public static AppConfig Load()
    {
        var cfg = new AppConfig();
        var path = Path.Combine(AppContext.BaseDirectory, "config.json");

        if (File.Exists(path))
        {
            try
            {
                var loaded = JsonSerializer.Deserialize<AppConfig>(
                    File.ReadAllText(path),
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (loaded != null) cfg = loaded;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Не удалось прочитать config.json:\n{ex.Message}",
                    "ЕСМА — Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Пароль можно задать переменной окружения — удобно, чтобы не хранить его в файле
        var envPassword = Environment.GetEnvironmentVariable("ESMA_PASSWORD");
        if (!string.IsNullOrEmpty(envPassword))
            cfg.ExcelPassword = envPassword;

        // Относительный путь считаем от папки с exe
        if (!Path.IsPathRooted(cfg.FilePath))
            cfg.FilePath = Path.Combine(AppContext.BaseDirectory, cfg.FilePath);

        return cfg;
    }
}
