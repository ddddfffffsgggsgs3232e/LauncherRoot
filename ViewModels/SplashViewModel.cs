using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;

namespace LauncherRoot.ViewModels;

public partial class SplashViewModel : ViewModelBase
{
    private readonly MainWindowViewModel _main;

    [ObservableProperty]
    private double _opacity;

    public SplashViewModel(MainWindowViewModel main)
    {
        _main = main;
        _ = AnimateAsync();
    }

    private async Task AnimateAsync()
    {
        for (double i = 0; i <= 1; i += 0.05)
        {
            Opacity = i;
            await Task.Delay(20);
        }
        Opacity = 1;

        await Task.Delay(3000);

        for (double i = 1; i >= 0; i -= 0.05)
        {
            Opacity = i;
            await Task.Delay(20);
        }
        Opacity = 0;

        _main.NavigateTo(PageType.MainMenu);
    }
}
