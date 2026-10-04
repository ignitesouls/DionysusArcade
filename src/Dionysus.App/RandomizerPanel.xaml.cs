// SPDX-License-Identifier: GPL-3.0-only
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;

namespace Dionysus.App;

// Shown after a randomizer build: everything the player hands to Matt's randomizer
public partial class RandomizerPanel : UserControl
{
    private string _optionsFile = "";
    private string _overlayDll = "";

    public RandomizerPanel()
    {
        InitializeComponent();
    }

    // Fills the panel from a build's outputs
    public void Show(IReadOnlyDictionary<string, string> outputs)
    {
        _optionsFile = outputs.GetValueOrDefault("randomizeopt", "");
        OptionsFileText.Text = Path.GetFileName(_optionsFile);
        ModFolderBox.Text = outputs.GetValueOrDefault("modfolder", "");

        // Matt's randomizer wants the folder containing the overlay DLL, not the DLL itself
        _overlayDll = outputs.GetValueOrDefault("overlay", "");
        OverlayBox.Text = _overlayDll.Length > 0 ? Path.GetDirectoryName(_overlayDll)! : "";
        OverlaySection.Visibility = _overlayDll.Length > 0 ? Visibility.Visible : Visibility.Collapsed;
        PanelStatus.Text = "";
    }

    private void SaveOptionsFile_Click(object sender, RoutedEventArgs e)
    {
        if (!File.Exists(_optionsFile))
        {
            PanelStatus.Text = "The options file is missing. Assemble again to recreate it.";
            return;
        }

        var dialog = new SaveFileDialog
        {
            FileName = Path.GetFileName(_optionsFile),
            Filter = "Randomizer options (*.randomizeopt)|*.randomizeopt",
            DefaultExt = ".randomizeopt"
        };
        if (dialog.ShowDialog() == true)
        {
            File.Copy(_optionsFile, dialog.FileName, overwrite: true);
            PanelStatus.Text = $"Saved to {dialog.FileName}";
        }
    }

    private void CopyModFolder_Click(object sender, RoutedEventArgs e) => CopyText(ModFolderBox.Text, "Mod folder path copied.");

    private void CopyOverlay_Click(object sender, RoutedEventArgs e) => CopyText(OverlayBox.Text, "Overlay path copied.");

    // Opens the overlay's folder with er_overlay.dll already selected
    // Opens the overlay's folder with er_overlay.dll already selected
    private void OpenOverlayFolder_Click(object sender, RoutedEventArgs e) =>
        Process.Start("explorer.exe", $"/select,\"{_overlayDll}\"");

    private void CopyText(string text, string message)
    {
        try
        {
            Clipboard.SetText(text);
            PanelStatus.Text = message;
        }
        catch (Exception)
        {
            // Another program can briefly lock the clipboard
            PanelStatus.Text = "Couldn't copy. Please try again.";
        }
    }
}