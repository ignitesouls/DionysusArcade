// SPDX-License-Identifier: GPL-3.0-only
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using Dionysus.App.Services;
using Dionysus.Core;
using Dionysus.Core.Modules;
using Dionysus.Core.Options;
using Dionysus.Core.Pipeline;
using Dionysus.Core.Presets;
using Dionysus.Core.Rando;
using Microsoft.Win32;

namespace Dionysus.App;

public partial class MainWindow : Wpf.Ui.Controls.FluentWindow
{
    private static readonly string ResourcesRoot = AppPaths.ResourcesRoot;

    private readonly AppSettings _settings = SettingsService.Load();

    // For each option key: a function that reads its control's value, and one that sets it
    private readonly Dictionary<string, Func<object?>> _optionReaders = new();
    private readonly Dictionary<string, Action<object>> _optionWriters = new();

    private List<Preset> _presets = new();

    // Fingerprint (seed + every option value) of the package on disk, or null if there isn't a usable one
    private string? _lastBuildFingerprint;

    // Files the last build produced, e.g. the randomizer options file
    private Dictionary<string, string> _lastOutputs = new();

    private bool _busy;

    public MainWindow()
    {
        InitializeComponent();
        Title = $"Dionysus Arcade v{AppInfo.Version}";
        MainTitleBar.Title = Title;

        // Keep the overlay's token in sync, e.g. after installing a new version of Dionysus
        try
        {
            OverlayConfigService.WriteToken(_settings.BattleshipToken);
        }
        catch (IOException)
        {
            // Not worth stopping startup over; saving the token in Settings reports any problem
        }

        BuildOptionsPanel(_settings.LastBuiltOptions);
        LoadPresetList();

        // Restore the last build, as long as its package is still on disk
        if (_settings.LastBuiltSeed is int seed
            && File.Exists(Path.Combine(ResourcesRoot, BuildPipeline.Me3Folder, BuildPipeline.ProfileFile)))
        {
            _lastBuildFingerprint = Fingerprint(seed, _settings.LastBuiltOptions);
            _lastOutputs = _settings.LastBuildOutputs;
            SeedBox.Text = seed.ToString();
            ModeSeedBox.Text = seed.ToString();
        }

        SeedBox.TextChanged += (_, _) => UpdateButtons();
        ModeSeedBox.TextChanged += (_, _) => UpdateButtons();
        ModeList.SelectionChanged += (_, _) => ShowSelectedMode();

        // Reselect the mode or profile used last time
        ModeList.SelectedItem = _presets.FirstOrDefault(p => p.DisplayName == _settings.LastMode) ?? _presets.FirstOrDefault();
        ShowSelectedMode();

        ThemeService.Apply(_settings.Theme, this);
    }

    // ---------- Options panel (Custom tab) ----------

    private void BuildOptionsPanel(IReadOnlyDictionary<string, object?> values)
    {
        var current = new OptionSet(ModuleRegistry.All.SelectMany(m => m.Options), values);

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

    // Rebuilds every option control, e.g. so a newly imported template appears in its dropdown
    private void RebuildOptionsPanel(IReadOnlyDictionary<string, object?> values)
    {
        OptionsPanel.Children.Clear();
        _optionReaders.Clear();
        _optionWriters.Clear();
        BuildOptionsPanel(values);
        UpdateButtons();
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
                    _optionWriters[option.Key] = v => box.IsChecked = (bool)v;
                    return box;
                }
            case IntOption:
                {
                    var box = new TextBox { Text = value.ToString(), Width = 120 };
                    box.TextChanged += (_, _) => MarkChanged();
                    _optionReaders[option.Key] = () => int.TryParse(box.Text, out int n) ? n : (object?)null;
                    _optionWriters[option.Key] = v => box.Text = v.ToString();
                    return Labeled(option, box);
                }
            case ChoiceOption choice:
                {
                    var combo = new ComboBox { ItemsSource = choice.CurrentChoices, SelectedItem = value, Width = 260 };
                    combo.SelectionChanged += (_, _) => MarkChanged();
                    _optionReaders[option.Key] = () => combo.SelectedItem as string;
                    _optionWriters[option.Key] = v => combo.SelectedItem = v;
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

    private Dictionary<string, object?> CurrentCustomOptions() =>
        _optionReaders.ToDictionary(kv => kv.Key, kv => kv.Value());

    // Puts a full set of option values into the controls. Anything not listed gets its default.
    private void ApplyOptions(IReadOnlyDictionary<string, object?> values)
    {
        var resolved = new OptionSet(ModuleRegistry.All.SelectMany(m => m.Options), values);
        foreach (var (key, value) in resolved.Values)
        {
            if (_optionWriters.TryGetValue(key, out Action<object>? write))
            {
                write(value);
            }
        }
    }

    private void ImportTemplateButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = "Import a template saved in Matt's randomizer",
            Filter = "Randomizer options (*.randomizeopt)|*.randomizeopt"
        };
        if (dialog.ShowDialog() != true) return;

        string name = RandomizerTemplates.Import(dialog.FileName);

        // Rebuild the options so the new template is listed, and select it
        Dictionary<string, object?> values = CurrentCustomOptions();
        values["randomizer.template"] = name;
        RebuildOptionsPanel(values);
        StatusText.Text = $"Imported template \"{name}\".";
    }

    // ---------- Modes and profiles ----------

    private void LoadPresetList()
    {
        _presets = PresetStore.LoadBuiltIn(ResourcesRoot)
            .Concat(PresetStore.LoadProfiles(AppPaths.ProfilesFolder))
            .ToList();
        PresetBox.ItemsSource = _presets;
        PresetBox.SelectedIndex = _presets.Count > 0 ? 0 : -1;
        ModeList.ItemsSource = _presets;
    }

    // Reloads modes and profiles, then selects the one stored at selectPath (or the first, if none)
    private void RefreshPresets(string? selectPath)
    {
        LoadPresetList();
        Preset? select = _presets.FirstOrDefault(p => string.Equals(p.FilePath, selectPath, StringComparison.OrdinalIgnoreCase));
        ModeList.SelectedItem = select ?? _presets.FirstOrDefault();
        if (select != null)
        {
            PresetBox.SelectedItem = select;
        }
    }

    private void ShowSelectedMode()
    {
        if (ModeList.SelectedItem is Preset preset)
        {
            ModeNameText.Text = preset.Name;
            ModeDescriptionText.Text = preset.Description.Length > 0 ? preset.Description
                : preset.IsBuiltIn ? "" : "Your saved profile.";
            ProfileActions.Visibility = preset.IsBuiltIn ? Visibility.Collapsed : Visibility.Visible;
            DeleteModeButton.Visibility = ProfileActions.Visibility;
            RenameBox.Text = "";

            _settings.LastMode = preset.DisplayName;
            SettingsService.Save(_settings);
        }
        else
        {
            ModeNameText.Text = "";
            ModeDescriptionText.Text = "";
            ProfileActions.Visibility = Visibility.Collapsed;
            DeleteModeButton.Visibility = Visibility.Collapsed;
        }
        UpdateButtons();
    }

    private void LoadPresetButton_Click(object sender, RoutedEventArgs e)
    {
        if (PresetBox.SelectedItem is Preset preset)
        {
            ApplyOptions(preset.Options);
        }
    }

    private void CustomizeButton_Click(object sender, RoutedEventArgs e)
    {
        if (ModeList.SelectedItem is Preset preset)
        {
            ApplyOptions(preset.Options);
            PresetBox.SelectedItem = preset;
            Tabs.SelectedIndex = 1; // the Custom tab
        }
    }

    private void SaveProfileButton_Click(object sender, RoutedEventArgs e)
    {
        string name = ProfileNameBox.Text.Trim();
        if (name.Length == 0)
        {
            StatusText.Text = "Type a name for the profile first.";
            return;
        }

        // Saving under an existing profile's name replaces it, so ask first
        bool exists = _presets.Any(p => !p.IsBuiltIn && string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase));
        if (exists && MessageBox.Show($"A profile called \"{name}\" already exists. Replace it?", "Dionysus Arcade",
                MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
        {
            return;
        }

        // Profiles save the current settings only, never the seed
        Preset saved = PresetStore.SaveProfile(AppPaths.ProfilesFolder, name, CurrentCustomOptions());
        RefreshPresets(saved.FilePath);
        ProfileNameBox.Text = "";
        StatusText.Text = $"Saved profile \"{name}\".";
    }

    private void RenameProfileButton_Click(object sender, RoutedEventArgs e)
    {
        if (ModeList.SelectedItem is not Preset profile || profile.IsBuiltIn) return;

        string newName = RenameBox.Text.Trim();
        if (newName.Length == 0)
        {
            ModeStatusText.Text = "Type the new name first.";
            return;
        }

        bool taken = _presets.Any(p => !p.IsBuiltIn && p != profile
            && string.Equals(p.Name, newName, StringComparison.OrdinalIgnoreCase));
        if (taken && MessageBox.Show($"A profile called \"{newName}\" already exists. Replace it?", "Dionysus Arcade",
                MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
        {
            return;
        }

        Preset renamed = PresetStore.RenameProfile(AppPaths.ProfilesFolder, profile, newName);
        RefreshPresets(renamed.FilePath);
    }

    private void DeleteProfileButton_Click(object sender, RoutedEventArgs e)
    {
        if (ModeList.SelectedItem is not Preset profile || profile.IsBuiltIn) return;

        if (MessageBox.Show($"Delete \"{profile.Name}\"?", "Dionysus Arcade",
                MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
        {
            return;
        }

        PresetStore.DeleteProfile(profile);
        RefreshPresets(null);
    }

    private void SettingsButton_Click(object sender, RoutedEventArgs e)
    {
        new SettingsWindow(_settings, this) { Owner = this }.ShowDialog();
    }

    // ---------- Build state ----------

    // Seed plus every option's final value, in a fixed order. Two builds with the same fingerprint are identical.
    private static string Fingerprint(int seed, IReadOnlyDictionary<string, object?> options)
    {
        var resolved = new OptionSet(ModuleRegistry.All.SelectMany(m => m.Options), options);
        var sorted = resolved.Values.OrderBy(kv => kv.Key, StringComparer.Ordinal).ToDictionary(kv => kv.Key, kv => kv.Value);
        return seed + "|" + JsonSerializer.Serialize(sorted);
    }

    // Is the package on disk exactly the build for this seed and these options?
    private bool IsBuilt(string seedText, IReadOnlyDictionary<string, object?> options) =>
        _lastBuildFingerprint != null
        && int.TryParse(seedText.Trim(), out int seed)
        && Fingerprint(seed, options) == _lastBuildFingerprint;

    private static bool UsesRandomizer(IReadOnlyDictionary<string, object?> options) =>
        new OptionSet(ModuleRegistry.All.SelectMany(m => m.Options), options).GetBool("randomizer.enabled");

    private void MarkChanged() => UpdateButtons();

    private void UpdateButtons()
    {
        Dictionary<string, object?> customOptions = CurrentCustomOptions();
        Preset? mode = ModeList.SelectedItem as Preset;

        bool customReady = !_busy && IsBuilt(SeedBox.Text, customOptions);
        bool modeReady = !_busy && mode != null && IsBuilt(ModeSeedBox.Text, mode.Options);
        bool customRandomizer = UsesRandomizer(customOptions);
        bool modeRandomizer = mode != null && UsesRandomizer(mode.Options);
        bool haveRandomizerOutput = _lastOutputs.ContainsKey("randomizeopt");

        // Assemble is only needed when the seed or options differ from what's already built
        RandomizeButton.IsEnabled = !_busy && !customReady;
        ModeRandomizeButton.IsEnabled = !_busy && mode != null && !modeReady;

        // Randomizer builds are launched from Matt's randomizer, so they show the randomizer panel instead of Launch
        LaunchButton.IsEnabled = customReady;
        ModeLaunchButton.IsEnabled = modeReady;
        LaunchButton.Visibility = customRandomizer ? Visibility.Collapsed : Visibility.Visible;
        ModeLaunchButton.Visibility = modeRandomizer ? Visibility.Collapsed : Visibility.Visible;
        ShowRandomizerPanel(CustomRandomizerPanel, customReady && customRandomizer && haveRandomizerOutput);
        ShowRandomizerPanel(ModeRandomizerPanel, modeReady && modeRandomizer && haveRandomizerOutput);

        SeedBox.IsEnabled = !_busy;
        ModeSeedBox.IsEnabled = !_busy;
        OptionsPanel.IsEnabled = !_busy;
        ModeList.IsEnabled = !_busy;

        if (!_busy)
        {
            StatusText.Text = StatusFor(customReady, customRandomizer, SeedBox.Text, "the mod with the current seed and options");
            ModeStatusText.Text = StatusFor(modeReady, modeRandomizer, ModeSeedBox.Text, "this mode");
        }
    }

    private void ShowRandomizerPanel(RandomizerPanel panel, bool show)
    {
        panel.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
        if (show)
        {
            panel.Show(_lastOutputs);
        }
    }

    private static string StatusFor(bool ready, bool randomizer, string seedText, string what) =>
        !ready ? $"Click Assemble to build {what}."
        : randomizer ? $"Assembled (seed {seedText.Trim()}). Set up Matt's randomizer with the files below."
        : $"Ready to launch (seed {seedText.Trim()}). Change the seed or options to assemble again.";

    // ---------- Building and launching ----------

    // Runs a build in the background. Returns the result, or null if it failed (the error appears in statusText).
    private async Task<BuildResult?> RunBuild(string seedText, IReadOnlyDictionary<string, object?> options, TextBlock statusText)
    {
        int? seed = null;
        if (seedText.Trim().Length > 0)
        {
            if (!int.TryParse(seedText.Trim(), out int parsed))
            {
                statusText.Text = "The seed must be a whole number, or empty for a random seed.";
                return null;
            }
            seed = parsed;
        }

        _busy = true;
        UpdateButtons();
        statusText.Text = "Assembling...";

        BuildResult? result = null;
        string? error = null;
        try
        {
            result = await Task.Run(() => BuildPipeline.Run(ResourcesRoot, seed, options));
            _lastOutputs = result.Outputs;
            _settings.LastBuiltSeed = result.Seed;
            _settings.LastBuiltOptions = options.ToDictionary(kv => kv.Key, kv => kv.Value);
            _settings.LastBuildOutputs = result.Outputs;
            SettingsService.Save(_settings);
            _lastBuildFingerprint = Fingerprint(result.Seed, options);
        }
        catch (Exception ex)
        {
            _lastBuildFingerprint = null; // the package on disk may be half-written
            _lastOutputs = new();
            error = ex.Message;
        }

        _busy = false;
        UpdateButtons();
        if (error != null)
        {
            statusText.Text = $"Assembling failed: {error}";
        }
        return result;
    }

    private static string FormatReport(BuildResult result) =>
        string.Join("\n", result.Report.Select(r => $"{r.Key}: {string.Join(" | ", r.Value)}"));

    private async void RandomizeButton_Click(object sender, RoutedEventArgs e)
    {
        BuildResult? result = await RunBuild(SeedBox.Text, CurrentCustomOptions(), StatusText);
        if (result != null)
        {
            SeedBox.Text = result.Seed.ToString();
            ReportText.Text = FormatReport(result);
        }
    }

    private async void ModeRandomizeButton_Click(object sender, RoutedEventArgs e)
    {
        if (ModeList.SelectedItem is not Preset preset) return;

        BuildResult? result = await RunBuild(ModeSeedBox.Text, preset.Options, ModeStatusText);
        if (result != null)
        {
            ModeSeedBox.Text = result.Seed.ToString();
            ModeReportText.Text = FormatReport(result);
        }
    }

    private void LaunchButton_Click(object sender, RoutedEventArgs e)
    {
        TextBlock status = ReferenceEquals(sender, ModeLaunchButton) ? ModeStatusText : StatusText;
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
            status.Text = "Launching Elden Ring...";
        }
        catch (Exception ex)
        {
            status.Text = $"Launch failed: {ex.Message}";
        }
    }
}