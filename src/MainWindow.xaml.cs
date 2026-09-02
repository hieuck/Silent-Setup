using System.Diagnostics;
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
    private readonly RemoteManifestService _remoteManifestService;
    private readonly AppConfig _config;

    private List<AppManifest> _apps = new();
    private List<AppManifest> _filteredApps = new();
    private List<PatchManifest> _patches = new();
    private Dictionary<string, CheckBox> _appCheckboxes = new();
    private Dictionary<string, List<CheckBox>> _patchCheckboxes = new();
    private Dictionary<string, Border> _appPanels = new();

    public MainWindow()
    {
        InitializeComponent();

        // Load config
        _config = LoadConfig();

        // Initialize services
        _logger = new LoggerService();
        _remoteManifestService = new RemoteManifestService(_logger);
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
        RefreshLogs();
    }

    private async Task LoadManifests()
    {
        StatusText.Text = "Loading manifests...";

        // Check and update cache if needed
        if (_remoteManifestService.IsCacheStale())
        {
            StatusText.Text = "Updating manifest cache from GitHub...";
            var updated = await _remoteManifestService.UpdateCacheAsync();
            if (updated)
            {
                _logger.Info("Cache updated successfully from GitHub");
            }
            else
            {
                _logger.Warn("Failed to update cache, using existing cache");
            }
        }

        await Task.Run(() =>
        {
            // Load remote apps from cache
            var cacheDir = _remoteManifestService.GetCacheDirectory();
            _apps = _manifestLoader.LoadApps(cacheDir);

            // Load local apps and merge
            var localAppsDir = _remoteManifestService.GetLocalAppsDirectory();
            var localApps = _manifestLoader.LoadApps(localAppsDir);

            // Merge: local apps override remote if same ID
            foreach (var localApp in localApps)
            {
                var existingIndex = _apps.FindIndex(a => a.Id == localApp.Id);
                if (existingIndex >= 0)
                {
                    _apps[existingIndex] = localApp;
                    _logger.Info($"Local app '{localApp.Name}' overrides remote");
                }
                else
                {
                    _apps.Add(localApp);
                    _logger.Info($"Added local app '{localApp.Name}'");
                }
            }

            // Load patches from cache/patches subfolder
            var patchesDir = Path.Combine(cacheDir, "patches");
            _patches = _manifestLoader.LoadPatches(patchesDir);

            // Load local patches and merge
            var localPatchesDir = _remoteManifestService.GetLocalPatchesDirectory();
            var localPatches = _manifestLoader.LoadPatches(localPatchesDir);

            foreach (var localPatch in localPatches)
            {
                var existingIndex = _patches.FindIndex(p => p.Id == localPatch.Id);
                if (existingIndex >= 0)
                {
                    _patches[existingIndex] = localPatch;
                }
                else
                {
                    _patches.Add(localPatch);
                }
            }

            _detectionService.RefreshAllApps(_apps);
        });

        StatusText.Text = $"Loaded {_apps.Count} apps, {_patches.Count} patches";
    }

    private void BuildUI()
    {
        AppListPanel.Children.Clear();
        _appCheckboxes.Clear();
        _patchCheckboxes.Clear();
        _appPanels.Clear();

        // Apply filters and sorting
        ApplyFiltersAndSort();

        if (!_filteredApps.Any())
        {
            AppListPanel.Children.Add(new TextBlock
            {
                Text = _apps.Any() ? "Không tìm thấy app phù hợp." : "No applications found in apps/ directory.",
                Margin = new Thickness(10),
                Foreground = System.Windows.Media.Brushes.Gray
            });
            return;
        }

        foreach (var app in _filteredApps)
        {
            var appPanel = CreateAppPanel(app);
            _appPanels[app.Id] = appPanel;
            AppListPanel.Children.Add(appPanel);
        }
    }

    private void ApplyFiltersAndSort()
    {
        _filteredApps = new List<AppManifest>(_apps);

        // Search filter
        var searchText = SearchTextBox?.Text?.ToLower() ?? "";
        if (!string.IsNullOrWhiteSpace(searchText))
        {
            _filteredApps = _filteredApps.Where(a =>
                a.Name.ToLower().Contains(searchText) ||
                (a.Metadata?.Category?.ToLower().Contains(searchText) ?? false) ||
                (a.Metadata?.Publisher?.ToLower().Contains(searchText) ?? false) ||
                (a.Metadata?.Tags?.Any(t => t.ToLower().Contains(searchText)) ?? false)
            ).ToList();
        }

        // Category filter
        var selectedCategory = (CategoryFilterComboBox?.SelectedItem as ComboBoxItem)?.Content?.ToString();
        if (!string.IsNullOrEmpty(selectedCategory) && selectedCategory != "Tất cả")
        {
            _filteredApps = _filteredApps.Where(a =>
                a.Metadata?.Category?.Equals(selectedCategory, StringComparison.OrdinalIgnoreCase) ?? false
            ).ToList();
        }

        // Sort
        var sortOption = (SortComboBox?.SelectedItem as ComboBoxItem)?.Content?.ToString();
        _filteredApps = sortOption switch
        {
            "Tên (A-Z)" => _filteredApps.OrderBy(a => a.Name).ToList(),
            "Tên (Z-A)" => _filteredApps.OrderByDescending(a => a.Name).ToList(),
            "Đã cài" => _filteredApps.OrderByDescending(a => a.Status == AppStatus.Installed).ThenBy(a => a.Name).ToList(),
            "Chưa cài" => _filteredApps.OrderBy(a => a.Status == AppStatus.Installed).ThenBy(a => a.Name).ToList(),
            "Category" => _filteredApps.OrderBy(a => a.Metadata?.Category ?? "").ThenBy(a => a.Name).ToList(),
            _ => _filteredApps.OrderBy(a => a.Name).ToList()
        };
    }

    private void SearchTextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
    {
        BuildUI();

        // Show/hide clear button
        if (ClearSearchButton != null)
        {
            ClearSearchButton.Visibility = string.IsNullOrEmpty(SearchTextBox.Text)
                ? Visibility.Collapsed
                : Visibility.Visible;
        }
    }

    private void ClearSearchButton_Click(object sender, RoutedEventArgs e)
    {
        SearchTextBox.Text = "";
        SearchTextBox.Focus();
    }

    private void CategoryFilterComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (_apps.Any()) // Only rebuild if apps are loaded
            BuildUI();
    }

    private void SortComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (_apps.Any()) // Only rebuild if apps are loaded
            BuildUI();
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

        // Add context menu
        var contextMenu = new ContextMenu();

        var editMenuItem = new MenuItem { Header = "Chỉnh sửa" };
        editMenuItem.Click += (s, e) => EditApp(app);
        contextMenu.Items.Add(editMenuItem);

        var deleteMenuItem = new MenuItem { Header = "Xóa" };
        deleteMenuItem.Click += (s, e) => DeleteApp(app);
        contextMenu.Items.Add(deleteMenuItem);

        contextMenu.Items.Add(new Separator());

        var detailsMenuItem = new MenuItem { Header = "Chi tiết" };
        detailsMenuItem.Click += (s, e) => ShowAppDetails(app);
        contextMenu.Items.Add(detailsMenuItem);

        var homepageMenuItem = new MenuItem { Header = "Mở trang chủ" };
        homepageMenuItem.Click += (s, e) => OpenHomepage(app);
        contextMenu.Items.Add(homepageMenuItem);

        border.ContextMenu = contextMenu;

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

    private void AddAppButton_Click(object sender, RoutedEventArgs e)
    {
        var result = MessageBox.Show(
            "Chọn cách thêm ứng dụng:\n\n" +
            "YES - Wizard (Tự động tìm và điền thông tin)\n" +
            "NO - Manual (Nhập thủ công tất cả thông tin)\n" +
            "CANCEL - Hủy bỏ",
            "Thêm Ứng dụng",
            MessageBoxButton.YesNoCancel,
            MessageBoxImage.Question);

        Window? dialog = result switch
        {
            MessageBoxResult.Yes => new AddAppWizard(),
            MessageBoxResult.No => new AddAppWindow(_remoteManifestService),
            _ => null
        };

        if (dialog?.ShowDialog() == true)
        {
            // Refresh to show new app
            _ = LoadManifests();
            BuildUI();
        }
    }

    private void SettingsButton_Click(object sender, RoutedEventArgs e)
    {
        var settingsWindow = new SettingsWindow(_patches);
        if (settingsWindow.ShowDialog() == true)
        {
            // Refresh patches if any changes were made
            _ = LoadManifests();
            BuildUI();
        }
    }

    private void ToggleLogsButton_Click(object sender, RoutedEventArgs e)
    {
        if (LogPanelColumn.Width.Value > 0)
        {
            // Hide log panel
            LogPanelColumn.Width = new GridLength(0);
            LogSplitter.Visibility = Visibility.Collapsed;
            LogPanel.Visibility = Visibility.Collapsed;
            ToggleLogsButton.Content = "Logs ❮";
        }
        else
        {
            // Show log panel
            LogPanelColumn.Width = new GridLength(400);
            LogSplitter.Visibility = Visibility.Visible;
            LogPanel.Visibility = Visibility.Visible;
            ToggleLogsButton.Content = "Logs ❯";
            RefreshLogs();
        }
    }

    private void RefreshLogsButton_Click(object sender, RoutedEventArgs e)
    {
        RefreshLogs();
    }

    private void ClearLogsButton_Click(object sender, RoutedEventArgs e)
    {
        LogTextBlock.Text = "";
    }

    private void OpenLogsFolderButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var logsDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
            if (Directory.Exists(logsDir))
            {
                Process.Start("explorer.exe", logsDir);
            }
            else
            {
                MessageBox.Show("Logs folder not found.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to open logs folder: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void HideLogsButton_Click(object sender, RoutedEventArgs e)
    {
        LogPanelColumn.Width = new GridLength(0);
        LogSplitter.Visibility = Visibility.Collapsed;
        LogPanel.Visibility = Visibility.Collapsed;
        ToggleLogsButton.Content = "Logs ❮";
    }

    private void RefreshLogs()
    {
        try
        {
            var logs = _logger.GetTodayLogs();
            LogTextBlock.Text = string.Join(Environment.NewLine, logs);

            // Auto-scroll to bottom
            LogScrollViewer.ScrollToEnd();
        }
        catch (Exception ex)
        {
            LogTextBlock.Text = $"Failed to load logs: {ex.Message}";
        }
    }

    // Context menu handlers
    private async void EditApp(AppManifest app)
    {
        try
        {
            var appsDir = Path.Combine(Directory.GetCurrentDirectory(), "apps");
            var filePath = Path.Combine(appsDir, $"{app.Id}.yaml");

            if (!File.Exists(filePath))
            {
                MessageBox.Show($"Không tìm thấy file: {filePath}", "Lỗi",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var editWindow = new EditAppWindow(app, filePath);
            if (editWindow.ShowDialog() == true)
            {
                // Refresh to show updated app
                await LoadManifests();
                BuildUI();
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Không thể chỉnh sửa app: {ex.Message}", "Lỗi",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void DeleteApp(AppManifest app)
    {
        var result = MessageBox.Show(
            $"Xóa app '{app.Name}'?\n\nFile YAML sẽ bị xóa vĩnh viễn.",
            "Xác nhận xóa",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result == MessageBoxResult.Yes)
        {
            try
            {
                var appsDir = Path.Combine(Directory.GetCurrentDirectory(), "apps");
                var filePath = Path.Combine(appsDir, $"{app.Id}.yaml");

                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                    MessageBox.Show($"Đã xóa {app.Name}", "Thành công",
                        MessageBoxButton.OK, MessageBoxImage.Information);

                    // Refresh
                    _ = LoadManifests();
                    BuildUI();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi xóa: {ex.Message}", "Lỗi",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private void ShowAppDetails(AppManifest app)
    {
        var details = $@"Tên: {app.Name}
ID: {app.Id}
Trang chủ: {app.Homepage}
Category: {app.Metadata?.Category ?? "N/A"}
Publisher: {app.Metadata?.Publisher ?? "N/A"}
License: {app.Metadata?.License ?? "N/A"}

Status: {app.Status}
Phiên bản đã cài: {app.InstalledVersion ?? "N/A"}
Phiên bản mới nhất: {app.LatestVersion ?? "N/A"}

Download URL: {app.Download.Url}
Install Type: {app.Install.Type}
Silent Args: {app.Install.SilentArgs}

Mô tả: {app.Metadata?.Description ?? "N/A"}";

        MessageBox.Show(details, $"Chi tiết: {app.Name}",
            MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void OpenHomepage(AppManifest app)
    {
        try
        {
            if (!string.IsNullOrWhiteSpace(app.Homepage))
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = app.Homepage,
                    UseShellExecute = true
                });
            }
            else
            {
                MessageBox.Show("App không có trang chủ.", "Thông báo",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Không thể mở browser: {ex.Message}", "Lỗi",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}