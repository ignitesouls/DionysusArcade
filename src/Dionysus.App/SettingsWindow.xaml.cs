// SPDX-License-Identifier: GPL-3.0-only
using System.IO;
using System.Windows;
using System.Windows.Controls;
using Dionysus.App.Services;

namespace Dionysus.App;

public partial class SettingsWindow : Wpf.Ui.Controls.FluentWindow
{
    private readonly AppSettings _settings;
    private readonly Window _mainWindow;

    // Receives the main window's settings object, so changes here are saved and seen everywhere
    public SettingsWindow(AppSettings settings, Window mainWindow)
    {
        InitializeComponent();
        _settings = settings;
        _mainWindow = mainWindow;

        ThemeBox.SelectedIndex = _settings.Theme switch { "Light" => 1, "Dark" => 2, _ => 0 };
        ThemeBox.SelectionChanged += (_, _) =>
        {
            _settings.Theme = ((ComboBoxItem)ThemeBox.SelectedItem).Content.ToString()!;
            SettingsService.Save(_settings);
            ThemeService.Apply(_settings.Theme, _mainWindow);
        };

        TokenBox.Text = _settings.BattleshipToken;
    }

    private void SaveTokenButton_Click(object sender, RoutedEventArgs e)
    {
        _settings.BattleshipToken = TokenBox.Text.Trim();
        SettingsService.Save(_settings);

        try
        {
            TokenStatusText.Text = OverlayConfigService.WriteToken(_settings.BattleshipToken)
                ? "Token saved."
                : "Token saved, but the overlay's config file wasn't found.";
        }
        catch (IOException ex)
        {
            TokenStatusText.Text = $"Token saved, but the overlay's config file couldn't be updated: {ex.Message}";
        }
    }
}