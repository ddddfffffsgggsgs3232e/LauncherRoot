using System.Collections.Generic;
using System.Threading.Tasks;
using LauncherRoot.Models;

namespace LauncherRoot.Services;

public interface IThemeService
{
    List<Theme> GetThemes();
    Task SaveThemesAsync(List<Theme> themes);
}
