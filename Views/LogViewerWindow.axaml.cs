using Avalonia.Controls;
using Avalonia.Interactivity;
using LauncherRoot.Services;
using System;
using System.IO;
using System.Linq;

namespace LauncherRoot.Views;

public partial class LogViewerWindow : Window
{
    private readonly IConfigService _config;

    public LogViewerWindow()
    {
        InitializeComponent();
        _config = new ConfigService();
        LoadLogContent();
    }

    private void LoadLogContent()
    {
        try
        {
            var logsDir = _config.GetLogsPath();
            if (!Directory.Exists(logsDir))
            {
                LogContent.Text = "(Henüz log dosyası yok.)";
                FooterText.Text = logsDir;
                return;
            }

            var today = $"launcher-{DateTime.Now:yyyy-MM-dd}.log";
            var logPath = Directory.GetFiles(logsDir, "launcher-*.log")
                                   .OrderByDescending(f => f)
                                   .FirstOrDefault();

            if (logPath == null)
            {
                LogContent.Text = "(Henüz log dosyası yok.)";
                FooterText.Text = logsDir;
                return;
            }

            var content = File.ReadAllText(logPath);
            LogContent.Text = string.IsNullOrWhiteSpace(content)
                ? "(Boş)"
                : content;
            FooterText.Text = logPath;
        }
        catch (Exception ex)
        {
            LogContent.Text = $"Hata: {ex.Message}";
        }
    }

    private void OnRefreshClick(object? sender, RoutedEventArgs e)
    {
        LoadLogContent();
    }
}
