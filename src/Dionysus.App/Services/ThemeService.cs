// SPDX-License-Identifier: GPL-3.0-only
using System.Windows;
using Wpf.Ui.Appearance;

namespace Dionysus.App.Services;

// Applies the light/dark theme to the whole app
public static class ThemeService
{
    private static bool _watchingSystem;

    public static void Apply(string theme, Window mainWindow)
    {
        if (theme == "System")
        {
            // Apply the current Windows setting now, then keep following it
            SystemThemeWatcher.Watch(mainWindow);
            ApplicationThemeManager.ApplySystemTheme();
            _watchingSystem = true;
            return;
        }

        if (_watchingSystem)
        {
            SystemThemeWatcher.UnWatch(mainWindow);
            _watchingSystem = false;
        }

        ApplicationThemeManager.Apply(theme == "Dark" ? ApplicationTheme.Dark : ApplicationTheme.Light);
    }
}