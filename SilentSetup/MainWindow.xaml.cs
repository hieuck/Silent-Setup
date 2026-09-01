using System.IO;
using System.Windows;
using System.Windows.Controls;
using Newtonsoft.Json;
using SilentSetup.Models;
using SilentSetup.Services;

namespace SilentSetup;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private readonly LoggerService _logger;
    private readonly ManifestLoader _manifestLoader;
    private readonly DetectionService _detectionService;
    private readonly DownloadService _downloadService;
    private readonly InstallService _installService;
    private readonly PatchService _patchService;
    private readonly AppConfig _config;

    private List<AppManifest> _apps = new();
    private List<PatchManifest> _patches = new();
    private Dictionary<string, CheckBox> _appCheckboxes = new();
    private Dictionary<string, List<CheckBox>> _patchCheckboxes = new();

    public MainWindow()
    {
        InitializeComponent();

        // Load config
        _config = LoadConfig();

        // Initialize services
        _logger = new LoggerService();
        _manifestLoader = new ManifestLoader(_logger);
        _detectionService = new DetectionService(_logger);
        _downloadService = new DownloadService(_logger, _config);
        _installService = new InstallService(_logger, _config);
        _patchService = new PatchService(_logger);

        _logger.Info("Silent Setup started");

        // Load manifests and build UI
        Loaded += MainWindow_Loaded;
    }

    private AppConfig LoadConfig()
    {
        try
        {
            var configPath = "config.json";
            if (File.Exists(configPath))
            {
                var json = File.ReadAllText(configPath);
                return JsonConvert.DeserializeObject<AppConfig>(json) ?? new AppConfig();
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to load config: {ex.Message}\nUsing defaults.", "Config Error",
                MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        return new AppConfig();
    }

    private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        await LoadManifests();
        BuildUI();
    }

    private async Task LoadManifests()
    {
        StatusText.Text = "Loading manifests...";

        await Task.Run(() =>
        {
            _apps = _manifestLoader.LoadApps();
            _patches = _manifestLoader.LoadPatches();
            _detectionService.RefreshAllApps(_apps);
        });

        StatusText.Text = $"Loaded {_apps.Count} apps, {_patches.Count} patches";
    }

    private void BuildUI()
    {
        AppListPanel.Children.Clear();
        _appCheckboxes.Clear();
        _patchCheckboxes.Clear();

        if (!_apps.Any())
        {
            AppListPanel.Children.Add(new TextBlock
            {
                Text = "No applications found in apps/ directory.",
                Margin = new Thickness(10),
                Foreground = System.Windows.Media.Brushes.Gray
            });
            return;
        }

        foreach (var app in _apps.OrderBy(a => a.Name))
        {
            var appPanel = CreateAppPanel(app);
            AppListPanel.Children.Add(appPanel);
        }
    }

    private Border CreateAppPanel(AppManifest app)
    {
        var border = new Border
        {
            BorderBrush = System.Windows.Media.Brushes.LightGray,
            BorderThickness = new Thickness(1),
            Margin = new Thickness(5),
            Padding = new Thickness(10),
            Background = System.Windows.Media.Brushes.White
        };

        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(30) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        // Checkbox
        var checkbox = new CheckBox
        {
            VerticalAlignment = VerticalAlignment.Center,
            IsEnabled = app.Status != AppStatus.Installed
        };
        _appCheckboxes[app.Id] = checkbox;
        Grid.SetColumn(checkbox, 0);
        grid.Children.Add(checkbox);

        // App info
        var infoPanel = new StackPanel();

        var nameText = new TextBlock
        {
            Text = app.Name,
            FontSize = 16,
            FontWeight = FontWeights.Bold
        };
        infoPanel.Children.Add(nameText);

        var detailsText = new TextBlock
        {
            Text = $"{app.Metadata?.Category ?? "Utility"} - {app.Metadata?.Publisher ?? "Unknown"}",
            FontSize = 12,
            Foreground = System.Windows.Media.Brushes.Gray
        };
        infoPanel.Children.Add(detailsText);

        // Patches
        var appPatches = _patches.Where(p => p.TargetApp == app.Id).ToList();
        if (appPatches.Any())
        {
            var patchesPanel = new StackPanel { Margin = new Thickness(20, 5, 0, 0) };
            _patchCheckboxes[app.Id] = new List<CheckBox>();

            foreach (var patch in appPatches)
            {
                var patchCheckbox = new CheckBox
                {
                    Content = patch.Name,
                    Margin = new Thickness(0, 2, 0, 2),
                    FontSize = 12
                };
                _patchCheckboxes[app.Id].Add(patchCheckbox);
                patchesPanel.Children.Add(patchCheckbox);
            }

            infoPanel.Children.Add(patchesPanel);
        }

        Grid.SetColumn(infoPanel, 1);
        grid.Children.Add(infoPanel);

        // Status
        var statusText = new TextBlock
        {
            Text = GetStatusText(app),
            VerticalAlignment = VerticalAlignment.Center,
            FontWeight = FontWeights.Bold,
            Foreground = GetStatusBrush(app.Status)
        };
        Grid.SetColumn(statusText, 2);
        grid.Children.Add(statusText);

        border.Child = grid;
        return border;
    }

    private string GetStatusText(AppManifest app)
    {
        return app.Status switch
        {
            AppStatus.Installed => $"Installed ({app.InstalledVersion})",
            AppStatus.NotInstalled => "Not Installed",
            AppStatus.UpdateAvailable => $"Update Available ({app.InstalledVersion} → {app.LatestVersion})",
            _ => "Unknown"
        };
    }

    private System.Windows.Media.Brush GetStatusBrush(AppStatus status)
    {
        return status switch
        {
            AppStatus.Installed => System.Windows.Media.Brushes.Green,
            AppStatus.NotInstalled => System.Windows.Media.Brushes.OrangeRed,
            AppStatus.UpdateAvailable => System.Windows.Media.Brushes.Orange,
            _ => System.Windows.Media.Brushes.Gray
        };
    }

    private async void RefreshButton_Click(object sender, RoutedEventArgs e)
    {
        await LoadManifests();
        BuildUI();
    }

    private void SelectAllButton_Click(object sender, RoutedEventArgs e)
    {
        foreach (var cb in _appCheckboxes.Values)
        {
            if (cb.IsEnabled)
                cb.IsChecked = true;
        }
    }

    private void DeselectAllButton_Click(object sender, RoutedEventArgs e)
    {
        foreach (var cb in _appCheckboxes.Values)
        {
            cb.IsChecked = false;
        }
    }

    private async void InstallButton_Click(object sender, RoutedEventArgs e)
    {
        var selectedApps = _apps.Where(a => _appCheckboxes[a.Id].IsChecked == true).ToList();

        if (!selectedApps.Any())
        {
            MessageBox.Show("Please select at least one application to install.", "No Selection",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        InstallButton.IsEnabled = false;

        try
        {
            await InstallSelectedApps(selectedApps);
        }
        finally
        {
            InstallButton.IsEnabled = true;
            ProgressBar.Value = 0;
            StatusText.Text = "Ready";
        }
    }

    private async Task InstallSelectedApps(List<AppManifest> apps)
    {
        var totalApps = apps.Count;
        var currentApp = 0;

        foreach (var app in apps)
        {
            currentApp++;
            StatusText.Text = $"[{currentApp}/{totalApps}] Processing {app.Name}...";
            ProgressBar.Value = 0;

            try
            {
                // Download
                StatusText.Text = $"[{currentApp}/{totalApps}] Downloading {app.Name}...";
                var progress = new Progress<int>(p => ProgressBar.Value = p);
                var downloadResult = await _downloadService.DownloadAsync(app, progress);

                if (!downloadResult.Success)
                {
                    _logger.Error($"Download failed for {app.Name}: {downloadResult.Message}");
                    MessageBox.Show($"Download failed for {app.Name}:\n{downloadResult.Message}",
                        "Download Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    continue;
                }

                // Install
                StatusText.Text = $"[{currentApp}/{totalApps}] Installing {app.Name}...";
                ProgressBar.IsIndeterminate = true;

                var installResult = await _installService.InstallAsync(app, downloadResult.FilePath!);

                ProgressBar.IsIndeterminate = false;

                if (!installResult.Success)
                {
                    _logger.Error($"Installation failed for {app.Name}: {installResult.Message}");
                    MessageBox.Show($"Installation failed for {app.Name}:\n{installResult.Message}\nExit code: {installResult.ExitCode}",
                        "Install Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    continue;
                }

                // Apply patches
                if (_patchCheckboxes.ContainsKey(app.Id))
                {
                    var selectedPatches = _patches
                        .Where(p => p.TargetApp == app.Id)
                        .Where((p, i) => _patchCheckboxes[app.Id][i].IsChecked == true)
                        .ToList();

                    foreach (var patch in selectedPatches)
                    {
                        StatusText.Text = $"[{currentApp}/{totalApps}] Applying patch: {patch.Name}...";
                        var patchResult = await _patchService.ApplyPatchAsync(patch, app);

                        if (!patchResult.Success)
                        {
                            _logger.Warn($"Patch failed for {patch.Name}: {patchResult.Message}");
                            MessageBox.Show($"Patch failed: {patch.Name}\n{patchResult.Message}",
                                "Patch Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                        }
                    }
                }

                _logger.Info($"Successfully installed: {app.Name}");
            }
            catch (Exception ex)
            {
                _logger.Error($"Error installing {app.Name}: {ex.Message}");
                MessageBox.Show($"Error installing {app.Name}:\n{ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        MessageBox.Show($"Installation complete!\n{totalApps} app(s) processed.", "Complete",
            MessageBoxButton.OK, MessageBoxImage.Information);

        // Refresh status
        await LoadManifests();
        BuildUI();
    }

    private void SettingsButton_Click(object sender, RoutedEventArgs e)
    {
        MessageBox.Show("Settings dialog not implemented yet.", "Settings",
            MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void ViewLogsButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var logs = _logger.GetTodayLogs();
            var logText = string.Join(Environment.NewLine, logs);

            var window = new Window
            {
                Title = "Logs",
                Width = 800,
                Height = 600,
                Content = new ScrollViewer
                {
                    Content = new TextBox
                    {
                        Text = logText,
                        IsReadOnly = true,
                        FontFamily = new System.Windows.Media.FontFamily("Consolas"),
                        VerticalScrollBarVisibility = ScrollBarVisibility.Auto
                    }
                }
            };
            window.ShowDialog();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to load logs: {ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}