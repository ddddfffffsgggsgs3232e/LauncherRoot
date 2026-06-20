using Avalonia.Controls;
using LauncherRoot.Services;
using LauncherRoot.ViewModels;

namespace LauncherRoot.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        var config = new ConfigService();
        var modrinth = new ModrinthService(config);
        var minecraft = new MinecraftService(config);
        var theme = new ThemeService(config);
        var localization = new LocalizationService();

        _ = ApplyStartupConfig(config, localization);

        DataContext = new MainWindowViewModel(config, modrinth, minecraft, theme, localization);
    }

    private static async Task ApplyStartupConfig(ConfigService config, LocalizationService localization)
    {
        var cfg = await config.LoadConfigAsync();

        App.SetTheme(cfg.DarkTheme);

        await localization.SetLanguageAsync(cfg.Language ?? "tr");
    }
}
