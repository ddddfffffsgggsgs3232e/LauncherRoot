using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Styling;
using LauncherRoot.Views;

namespace LauncherRoot;

public partial class App : Application
{
    public static void SetTheme(bool isDark)
    {
        if (Current is App app)
        {
            app.RequestedThemeVariant = isDark ? ThemeVariant.Dark : ThemeVariant.Light;

            var resources = app.Resources;
            if (isDark)
            {
                resources["BgPrimaryBrush"] = new SolidColorBrush(Color.Parse("#0A1A0F"));
                resources["BgSecondaryBrush"] = new SolidColorBrush(Color.Parse("#0F2418"));
                resources["BgCardBrush"] = new SolidColorBrush(Color.Parse("#162E1F"));
                resources["BgHoverBrush"] = new SolidColorBrush(Color.Parse("#1D3826"));
                resources["BgSidebarBrush"] = new SolidColorBrush(Color.Parse("#0A1A0F"));
                resources["AccentBrush"] = new SolidColorBrush(Color.Parse("#4ADE80"));
                resources["AccentHoverBrush"] = new SolidColorBrush(Color.Parse("#6EE7A0"));
                resources["SuccessBrush"] = new SolidColorBrush(Color.Parse("#34D399"));
                resources["WarningBrush"] = new SolidColorBrush(Color.Parse("#FBBF24"));
                resources["ErrorBrush"] = new SolidColorBrush(Color.Parse("#F87171"));
                resources["TextPrimaryBrush"] = new SolidColorBrush(Color.Parse("#ECFDF5"));
                resources["TextSecondaryBrush"] = new SolidColorBrush(Color.Parse("#A7F3D0"));
                resources["TextMutedBrush"] = new SolidColorBrush(Color.Parse("#6B7280"));
                resources["BorderBrush"] = new SolidColorBrush(Color.Parse("#1F3A2B"));
            }
            else
            {
                resources["BgPrimaryBrush"] = new SolidColorBrush(Color.Parse("#F0FDF4"));
                resources["BgSecondaryBrush"] = new SolidColorBrush(Color.Parse("#DCFCE7"));
                resources["BgCardBrush"] = new SolidColorBrush(Color.Parse("#ECFDF5"));
                resources["BgHoverBrush"] = new SolidColorBrush(Color.Parse("#BBF7D0"));
                resources["BgSidebarBrush"] = new SolidColorBrush(Color.Parse("#F0FDF4"));
                resources["AccentBrush"] = new SolidColorBrush(Color.Parse("#16A34A"));
                resources["AccentHoverBrush"] = new SolidColorBrush(Color.Parse("#15803D"));
                resources["SuccessBrush"] = new SolidColorBrush(Color.Parse("#059669"));
                resources["WarningBrush"] = new SolidColorBrush(Color.Parse("#D97706"));
                resources["ErrorBrush"] = new SolidColorBrush(Color.Parse("#DC2626"));
                resources["TextPrimaryBrush"] = new SolidColorBrush(Color.Parse("#052E16"));
                resources["TextSecondaryBrush"] = new SolidColorBrush(Color.Parse("#166534"));
                resources["TextMutedBrush"] = new SolidColorBrush(Color.Parse("#6B7280"));
                resources["BorderBrush"] = new SolidColorBrush(Color.Parse("#BBF7D0"));
            }
        }
    }

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow();
        }
        base.OnFrameworkInitializationCompleted();
    }
}
