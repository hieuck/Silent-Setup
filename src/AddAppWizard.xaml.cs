using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.IO;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using SilentSetup.Services;
using SilentSetup.Models;

namespace SilentSetup;

public partial class AddAppWizard : Window
{
    private int _currentStep = 1;
    private PackageSearchService _searchService;
    private List<PackageSearchResult> _searchResults = new();
    private PackageSearchResult? _selectedPackage;
    private readonly ISerializer _yamlSerializer;

    public AddAppWizard()
    {
        InitializeComponent();
        _searchService = new PackageSearchService();
        _yamlSerializer = new SerializerBuilder()
            .WithNamingConvention(UnderscoredNamingConvention.Instance)
            .Build();
        UpdateStepUI();
    }

    private void UpdateStepUI()
    {
        // Hide all panels
        Step1Panel.Visibility = Visibility.Collapsed;
        Step2Panel.Visibility = Visibility.Collapsed;
        Step3Panel.Visibility = Visibility.Collapsed;
        Step4Panel.Visibility = Visibility.Collapsed;

        // Reset all step indicators
        Step1Circle.Fill = new SolidColorBrush(Color.FromRgb(0xBD, 0xC3, 0xC7));
        Step2Circle.Fill = new SolidColorBrush(Color.FromRgb(0xBD, 0xC3, 0xC7));
        Step3Circle.Fill = new SolidColorBrush(Color.FromRgb(0xBD, 0xC3, 0xC7));
        Step4Circle.Fill = new SolidColorBrush(Color.FromRgb(0xBD, 0xC3, 0xC7));

        Step1Text.Foreground = Brushes.Gray;
        Step2Text.Foreground = Brushes.Gray;
        Step3Text.Foreground = Brushes.Gray;
        Step4Text.Foreground = Brushes.Gray;

        Step1Text.FontWeight = FontWeights.Normal;
        Step2Text.FontWeight = FontWeights.Normal;
        Step3Text.FontWeight = FontWeights.Normal;
        Step4Text.FontWeight = FontWeights.Normal;

        // Show current step
        switch (_currentStep)
        {
            case 1:
                Step1Panel.Visibility = Visibility.Visible;
                Step1Circle.Fill = new SolidColorBrush(Color.FromRgb(0x34, 0x98, 0xDB));
                Step1Text.Foreground = Brushes.Black;
                Step1Text.FontWeight = FontWeights.Bold;
                BackButton.Visibility = Visibility.Collapsed;
                NextButton.Visibility = Visibility.Collapsed;
                FinishButton.Visibility = Visibility.Collapsed;
                break;

            case 2:
                Step2Panel.Visibility = Visibility.Visible;
                Step2Circle.Fill = new SolidColorBrush(Color.FromRgb(0x34, 0x98, 0xDB));
                Step2Text.Foreground = Brushes.Black;
                Step2Text.FontWeight = FontWeights.Bold;
                BackButton.Visibility = Visibility.Visible;
                NextButton.Visibility = Visibility.Collapsed;
                FinishButton.Visibility = Visibility.Collapsed;
                break;

            case 3:
                Step3Panel.Visibility = Visibility.Visible;
                Step3Circle.Fill = new SolidColorBrush(Color.FromRgb(0x34, 0x98, 0xDB));
                Step3Text.Foreground = Brushes.Black;
                Step3Text.FontWeight = FontWeights.Bold;
                BackButton.Visibility = Visibility.Visible;
                NextButton.Visibility = Visibility.Visible;
                FinishButton.Visibility = Visibility.Collapsed;
                break;

            case 4:
                Step4Panel.Visibility = Visibility.Visible;
                Step4Circle.Fill = new SolidColorBrush(Color.FromRgb(0x27, 0xAE, 0x60));
                Step4Text.Foreground = Brushes.Black;
                Step4Text.FontWeight = FontWeights.Bold;
                BackButton.Visibility = Visibility.Collapsed;
                NextButton.Visibility = Visibility.Collapsed;
                FinishButton.Visibility = Visibility.Visible;
                break;
        }
    }

    private async void SearchButton_Click(object sender, RoutedEventArgs e)
    {
        var query = SearchTextBox.Text.Trim();
        if (string.IsNullOrEmpty(query))
        {
            MessageBox.Show("Vui lòng nhập tên phần mềm hoặc URL trang chủ.", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        SearchButton.IsEnabled = false;
        SearchStatusText.Text = "Đang tìm kiếm...";

        try
        {
            _searchResults = await _searchService.SearchPackagesAsync(query);

            if (_searchResults.Count == 0)
            {
                SearchStatusText.Text = "Không tìm thấy kết quả nào. Thử từ khóa khác hoặc dùng AddApp thủ công.";
                return;
            }

            SearchStatusText.Text = $"Tìm thấy {_searchResults.Count} kết quả";

            // Move to step 2 and display results
            _currentStep = 2;
            DisplaySearchResults();
            UpdateStepUI();
        }
        catch (Exception ex)
        {
            SearchStatusText.Text = $"Lỗi tìm kiếm: {ex.Message}";
            MessageBox.Show($"Lỗi tìm kiếm: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            SearchButton.IsEnabled = true;
        }
    }

    private void DisplaySearchResults()
    {
        ResultsPanel.Children.Clear();

        foreach (var result in _searchResults)
        {
            var resultCard = new Border
            {
                BorderBrush = new SolidColorBrush(Color.FromRgb(0xBD, 0xC3, 0xC7)),
                BorderThickness = new Thickness(1),
                Padding = new Thickness(15),
                Margin = new Thickness(0, 0, 0, 10),
                Background = Brushes.White
            };

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var infoPanel = new StackPanel();

            var nameText = new TextBlock
            {
                Text = result.Name,
                FontSize = 16,
                FontWeight = FontWeights.Bold
            };
            infoPanel.Children.Add(nameText);

            var descText = new TextBlock
            {
                Text = result.Description,
                TextWrapping = TextWrapping.Wrap,
                Foreground = Brushes.Gray,
                Margin = new Thickness(0, 5, 0, 5)
            };
            infoPanel.Children.Add(descText);

            var metaText = new TextBlock
            {
                Text = $"Nguồn: {result.Source} | ID: {result.Id} | Publisher: {result.Publisher}",
                FontSize = 11,
                Foreground = Brushes.Gray
            };
            infoPanel.Children.Add(metaText);

            grid.Children.Add(infoPanel);
            Grid.SetColumn(infoPanel, 0);

            var selectBtn = new Button
            {
                Content = "Chọn →",
                Width = 100,
                Height = 35,
                Background = new SolidColorBrush(Color.FromRgb(0x34, 0x98, 0xDB)),
                Foreground = Brushes.White,
                Tag = result
            };
            selectBtn.Click += SelectResultButton_Click;
            grid.Children.Add(selectBtn);
            Grid.SetColumn(selectBtn, 1);

            resultCard.Child = grid;
            ResultsPanel.Children.Add(resultCard);
        }
    }

    private async void SelectResultButton_Click(object sender, RoutedEventArgs e)
    {
        var button = sender as Button;
        if (button?.Tag is PackageSearchResult result)
        {
            _selectedPackage = result;

            // Fetch more details if needed
            button.IsEnabled = false;
            button.Content = "Đang tải...";

            try
            {
                _selectedPackage = await _searchService.FetchPackageDetailsAsync(result);

                // Move to step 3 and populate form
                _currentStep = 3;
                PopulateCustomizeForm();
                UpdateStepUI();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi lấy thông tin: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                button.IsEnabled = true;
                button.Content = "Chọn →";
            }
        }
    }

    private void PopulateCustomizeForm()
    {
        if (_selectedPackage == null) return;

        FinalNameTextBox.Text = _selectedPackage.Name;
        FinalIdTextBox.Text = _selectedPackage.Id;
        FinalDownloadTextBox.Text = _selectedPackage.DownloadUrl;
        FinalSilentArgsTextBox.Text = _selectedPackage.SilentArgs;
        FinalDescriptionTextBox.Text = _selectedPackage.Description;
        SourceInfoText.Text = $"Nguồn: {_selectedPackage.Source} | Version: {_selectedPackage.Version} | Publisher: {_selectedPackage.Publisher}";

        // Set installer type
        var typeIndex = _selectedPackage.InstallerType.ToLower() switch
        {
            "exe" => 0,
            "msi" => 1,
            "zip" => 2,
            _ => 0
        };
        FinalTypeComboBox.SelectedIndex = typeIndex;
    }

    private void BackButton_Click(object sender, RoutedEventArgs e)
    {
        if (_currentStep > 1)
        {
            _currentStep--;
            UpdateStepUI();
        }
    }

    private void NextButton_Click(object sender, RoutedEventArgs e)
    {
        // Validate step 3 before proceeding
        if (_currentStep == 3)
        {
            if (string.IsNullOrWhiteSpace(FinalNameTextBox.Text) ||
                string.IsNullOrWhiteSpace(FinalIdTextBox.Text) ||
                string.IsNullOrWhiteSpace(FinalDownloadTextBox.Text))
            {
                MessageBox.Show("Vui lòng điền đầy đủ thông tin bắt buộc.", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Save manifest
            try
            {
                SaveManifest();
                _currentStep = 4;
                FinishMessageText.Text = $"Đã tạo manifest cho '{FinalNameTextBox.Text}' thành công!";
                UpdateStepUI();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi tạo manifest: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private void SaveManifest()
    {
        var manifest = new AppManifest
        {
            Name = FinalNameTextBox.Text,
            Id = FinalIdTextBox.Text,
            Homepage = "https://example.com", // TODO: Get from source
            Download = new DownloadConfig
            {
                Url = FinalDownloadTextBox.Text
            },
            Install = new InstallConfig
            {
                Type = (FinalTypeComboBox.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "exe",
                SilentArgs = FinalSilentArgsTextBox.Text
            },
            Detection = new DetectionConfig
            {
                Registry = new List<RegistryDetection>(),
                File = new List<FileDetection>()
            },
            Metadata = new MetadataConfig
            {
                Description = FinalDescriptionTextBox.Text,
                Category = "Other",
                Publisher = _selectedPackage?.Publisher ?? "",
                License = "Unknown"
            }
        };

        // Serialize to YAML
        var yaml = _yamlSerializer.Serialize(manifest);

        // Save to file
        var filePath = Path.Combine("apps", $"{manifest.Id}.yaml");
        Directory.CreateDirectory("apps");
        File.WriteAllText(filePath, yaml);
    }

    private void FinishButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
        Close();
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
