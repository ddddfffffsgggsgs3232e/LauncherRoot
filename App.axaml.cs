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
                resources["BgPrimaryBrush"] = new SolidColorBrush(Color.Parse("#0D1117"));
                resources["BgSecondaryBrush"] = new SolidColorBrush(Color.Parse("#161B22"));
                resources["BgCardBrush"] = new SolidColorBrush(Color.Parse("#1C2128"));
                resources["BgHoverBrush"] = new SolidColorBrush(Color.Parse("#21262D"));
                resources["BgSidebarBrush"] = new SolidColorBrush(Color.Parse("#0D1117"));
                resources["AccentBrush"] = new SolidColorBrush(Color.Parse("#58A6FF"));
                resources["AccentHoverBrush"] = new SolidColorBrush(Color.Parse("#79C0FF"));
                resources["SuccessBrush"] = new SolidColorBrush(Color.Parse("#3FB950"));
                resources["WarningBrush"] = new SolidColorBrush(Color.Parse("#D29922"));
                resources["ErrorBrush"] = new SolidColorBrush(Color.Parse("#F85149"));
                resources["TextPrimaryBrush"] = new SolidColorBrush(Color.Parse("#E6EDF3"));
                resources["TextSecondaryBrush"] = new SolidColorBrush(Color.Parse("#8B949E"));
                resources["TextMutedBrush"] = new SolidColorBrush(Color.Parse("#484F58"));
                resources["BorderBrush"] = new SolidColorBrush(Color.Parse("#30363D"));
            }
            else
            {
                resources["BgPrimaryBrush"] = new SolidColorBrush(Color.Parse("#FFFFFF"));
                resources["BgSecondaryBrush"] = new SolidColorBrush(Color.Parse("#F6F8FA"));
                resources["BgCardBrush"] = new SolidColorBrush(Color.Parse("#F0F2F5"));
                resources["BgHoverBrush"] = new SolidColorBrush(Color.Parse("#E8ECF0"));
                resources["BgSidebarBrush"] = new SolidColorBrush(Color.Parse("#F6F8FA"));
                resources["AccentBrush"] = new SolidColorBrush(Color.Parse("#0969DA"));
                resources["AccentHoverBrush"] = new SolidColorBrush(Color.Parse("#0550AE"));
                resources["SuccessBrush"] = new SolidColorBrush(Color.Parse("#1A7F37"));
                resources["WarningBrush"] = new SolidColorBrush(Color.Parse("#9A6700"));
                resources["ErrorBrush"] = new SolidColorBrush(Color.Parse("#CF222E"));
                resources["TextPrimaryBrush"] = new SolidColorBrush(Color.Parse("#1F2328"));
                resources["TextSecondaryBrush"] = new SolidColorBrush(Color.Parse("#656D76"));
                resources["TextMutedBrush"] = new SolidColorBrush(Color.Parse("#8C959F"));
                resources["BorderBrush"] = new SolidColorBrush(Color.Parse("#D0D7DE"));
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
