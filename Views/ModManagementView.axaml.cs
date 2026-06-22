using Avalonia.Controls;
using Avalonia.Interactivity;
using LauncherRoot.Models;

namespace LauncherRoot.Views;

public partial class ModManagementView : UserControl
{
    public ModManagementView()
    {
        InitializeComponent();
    }

    private void ToggleSwitch_IsCheckedChanged(object? sender, RoutedEventArgs e)
    {
        if (sender is ToggleSwitch toggle && toggle.DataContext is ModInfo mod)
        {
            if (DataContext is ViewModels.ModManagementViewModel vm)
            {
                vm.ToggleModCommand.Execute(mod);
            }
        }
    }
}
