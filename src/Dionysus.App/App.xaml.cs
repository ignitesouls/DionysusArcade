// SPDX-License-Identifier: GPL-3.0-only
using System.IO;
using System.Windows;

namespace Dionysus.App;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        // Show any unexpected error in a message box instead of closing silently
        DispatcherUnhandledException += (_, args) =>
        {
            MessageBox.Show(args.Exception.ToString(), "Dionysus Arcade error",
                MessageBoxButton.OK, MessageBoxImage.Error);
            args.Handled = true;
            Shutdown();
        };

        // SoulsFormats and ParamsEditor find their files relative to the working directory,
        // so always run from the app's own folder, however the app was launched.
        Directory.SetCurrentDirectory(AppPaths.AppFolder);
        base.OnStartup(e);
    }
}