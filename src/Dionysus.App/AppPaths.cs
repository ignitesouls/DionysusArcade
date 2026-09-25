// SPDX-License-Identifier: GPL-3.0-only
using System.IO;

namespace Dionysus.App;

public static class AppPaths
{
    // The folder containing Dionysus Arcade.exe. AppContext.BaseDirectory can't be used here:
    // in the single-file build, it points to the temporary folder the exe unpacks itself into.
    public static string AppFolder { get; } = Path.GetDirectoryName(Environment.ProcessPath)!;

    public static string ResourcesRoot { get; } = Path.Combine(AppFolder, "Resources");
}