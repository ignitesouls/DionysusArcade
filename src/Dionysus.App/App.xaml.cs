// SPDX-License-Identifier: GPL-3.0-only
using System.IO;
using System.Windows;

namespace Dionysus.App;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        // SoulsFormats and ParamsEditor find their files relative to the working directory,
        // so always run from the app's own folder, however the app was launched.
        Directory.SetCurrentDirectory(AppContext.BaseDirectory);
        base.OnStartup(e);
    }
}