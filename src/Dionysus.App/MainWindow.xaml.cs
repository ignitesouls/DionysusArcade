// SPDX-License-Identifier: GPL-3.0-only
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using Dionysus.App.Services;
using Dionysus.Core.Modules;
using Dionysus.Core.Options;
using Dionysus.Core.Pipeline;
using Wpf.Ui.Appearance;

namespace Dionysus.App;

public partial class MainWindow : Wpf.Ui.Controls.FluentWindow
{
    private static readonly string ResourcesRoot = AppPaths.ResourcesRoot;

    private readonly AppSettings _settings = SettingsService.Load();

    // For each option key, a function that reads the current value from its control
    private readonly Dictionary<string, Func<object?>> _optionReaders = new();

    private bool _buildIsCurrent;
    private bool _busy;
    private bool _suppressChanges;

    public MainWindow()
    {
        InitializeComponent();

        BuildOptionsPanel();
        SeedBox.Text = _settings.LastBuiltSeed?.ToString() ?? "";
        SeedBox.TextChanged += (_, _) => MarkChanged();

        // If there was a previous build, the package on disk matches the restored seed and options
        _buildIsCurrent = _settings.LastBuiltSeed != null && File.Exists(Path.Combine(ResourcesRoot, BuildPipeline.Me3Folder, BuildPipeline.ProfileFile));
        UpdateButtons();

        SetUpTheme();
    }

    // ---------- Options panel ----------

    private void BuildOptionsPanel()
    {
        // Start from the options of the last build (or defaults, if there isn't one)
        var current = new OptionSet(ModuleRegistry.All.SelectMany(m => m.Options), _settings.LastBuiltOptions);

        foreach (IModule module in ModuleRegistry.All)
        {
            var group = new StackPanel { Margin = new Thickness(8) };

            foreach (OptionDefinition option in module.Options)
            {
                group.Children.Add(CreateControl(option, current.Values[option.Key]));
            }

            OptionsPanel.Children.Add(new GroupBox
            {
                Header = module.DisplayName,
                Content = group,
                Margin = new Thickness(0, 0, 0, 8)
            });
        }
    }

    private UIElement CreateControl(OptionDefinition option, object value)
    {
        switch (option)
        {
            case BoolOption:
                {
                    var box = new CheckBox
                    {
                        Content = option.Label,
                        IsChecked = (bool)value,
                        ToolTip = Tip(option),
                        Margin = new Thickness(0, 2, 0, 2)
                    };
                    box.Checked += (_, _) => MarkChanged();
                    box.Unchecked += (_, _) => MarkChanged();
                    _optionReaders[option.Key] = () => box.IsChecked == true;
                    return box;
                }
            case IntOption:
                {
                    var box = new TextBox { Text = value.ToString(), Width = 120 };
                    box.TextChanged += (_, _) => MarkChanged();
                    _optionReaders[option.Key] = () => int.TryParse(box.Text, out int n) ? n : (object?)null;
                    return Labeled(option, box);
                }
            case ChoiceOption choice:
                {
                    var combo = new ComboBox { ItemsSource = choice.Choices, SelectedItem = value, Width = 260 };
                    combo.SelectionChanged += (_, _) => MarkChanged();
                    _optionReaders[option.Key] = () => combo.SelectedItem as string;
                    return Labeled(option, combo);
                }
            default:
                throw new NotSupportedException($"No control for option type {option.GetType().Name}");
        }
    }

    private static UIElement Labeled(OptionDefinition option, Control control)
    {
        var row = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Margin = new Thickness(0, 2, 0, 2),
            ToolTip = Tip(option)
        };
        row.Children.Add(new TextBlock { Text = option.Label + ":", Width = 180, VerticalAlignment = VerticalAlignment.Center });
        row.Children.Add(control);
        return row;
    }

    private static object? Tip(OptionDefinition option) =>
        string.IsNullOrEmpty(option.Description) ? null : option.Description;

    // ---------- State ----------

    private void MarkChanged()
    {
        if (_suppressChanges) return;
        _buildIsCurrent = false;
        UpdateButtons();
    }

    private void UpdateButtons()
    {
        RandomizeButton.IsEnabled = !_busy;
        LaunchButton.IsEnabled = !_busy && _buildIsCurrent;
        SeedBox.IsEnabled = !_busy;
        OptionsPanel.IsEnabled = !_busy;

        if (!_busy)
        {
            StatusText.Text = _buildIsCurrent
                ? $"Ready to launch (seed {_settings.LastBuiltSeed})."
                : "Click Randomize to build the mod with the current seed and options.";
        }
    }

    // ---------- Theme ----------

    private bool _watchingSystemTheme;

    private void SetUpTheme()
    {
        ThemeBox.SelectedIndex = _settings.Theme switch { "Light" => 1, "Dark" => 2, _ => 0 };
        ApplyTheme(_settings.Theme);

        ThemeBox.SelectionChanged += (_, _) =>
        {
            _settings.Theme = ((ComboBoxItem)ThemeBox.SelectedItem).Content.ToString()!;
            SettingsService.Save(_settings);
            ApplyTheme(_settings.Theme);
        };
    }

    private void ApplyTheme(string theme)
    {
        if (theme == "System")
        {
            // Apply the current Windows setting now, then keep following it
            SystemThemeWatcher.Watch(this);
            ApplicationThemeManager.ApplySystemTheme();
            _watchingSystemTheme = true;
            return;
        }

        if (_watchingSystemTheme)
        {
            SystemThemeWatcher.UnWatch(this);
            _watchingSystemTheme = false;
        }

        ApplicationThemeManager.Apply(theme == "Dark" ? ApplicationTheme.Dark : ApplicationTheme.Light);
    }

    // ---------- Buttons ----------

    private async void RandomizeButton_Click(object sender, RoutedEventArgs e)
    {
        string seedText = SeedBox.Text.Trim();
        int? seed = null;
        if (seedText.Length > 0)
        {
            if (!int.TryParse(seedText, out int parsed))
            {
                StatusText.Text = "The seed must be a whole number, or empty for a random seed.";
                return;
            }
            seed = parsed;
        }

        Dictionary<string, object?> options = _optionReaders.ToDictionary(kv => kv.Key, kv => kv.Value());

        _busy = true;
        UpdateButtons();
        StatusText.Text = "Randomizing...";

        string? error = null;
        try
        {
            BuildResult result = await Task.Run(() => BuildPipeline.Run(ResourcesRoot, seed, options));

            _suppressChanges = true;
            SeedBox.Text = result.Seed.ToString();
            _suppressChanges = false;

            ReportText.Text = string.Join("\n",
                result.Report.Select(r => $"{r.Key}: {string.Join(" | ", r.Value)}"));

            _settings.LastBuiltSeed = result.Seed;
            _settings.LastBuiltOptions = options;
            SettingsService.Save(_settings);
            _buildIsCurrent = true;
        }
        catch (Exception ex)
        {
            _buildIsCurrent = false;
            error = ex.Message;
        }

        _busy = false;
        UpdateButtons();

        if (error != null)
        {
            StatusText.Text = $"Randomizing failed: {error}";
        }
    }

    private void LaunchButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = "/c \"launch-dionysus.bat\"",
                WorkingDirectory = Path.Combine(ResourcesRoot, BuildPipeline.Me3Folder),
                UseShellExecute = false,
                CreateNoWindow = true
            });
            StatusText.Text = "Launching Elden Ring...";
        }
        catch (Exception ex)
        {
            StatusText.Text = $"Launch failed: {ex.Message}";
        }
    }
}