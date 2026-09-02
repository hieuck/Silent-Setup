using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using SilentSetup.Models;
using SilentSetup.Services;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace SilentSetup
{
    public partial class AddAppWindow : Window
    {
        private readonly RemoteManifestService _remoteManifestService;

        public AddAppWindow(RemoteManifestService remoteManifestService)
        {
            InitializeComponent();
            _remoteManifestService = remoteManifestService;
        }

        private void IdTextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            // Auto-normalize ID: lowercase, replace spaces with hyphens, remove special chars
            var text = IdTextBox.Text;
            var normalized = text.ToLower()
                .Replace(" ", "-")
                .Replace("_", "-");

            // Remove any characters that aren't alphanumeric or hyphen
            normalized = Regex.Replace(normalized, @"[^a-z0-9\-]", "");

            // Remove consecutive hyphens
            normalized = Regex.Replace(normalized, @"-+", "-");

            // Remove leading/trailing hyphens
            normalized = normalized.Trim('-');

            if (normalized != text)
            {
                var cursorPos = IdTextBox.CaretIndex;
                IdTextBox.Text = normalized;
                IdTextBox.CaretIndex = Math.Min(cursorPos, normalized.Length);
            }
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            // Validate required fields
            if (string.IsNullOrWhiteSpace(NameTextBox.Text))
            {
                MessageBox.Show("Vui lòng nhập tên ứng dụng.", "Thiếu thông tin", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(IdTextBox.Text))
            {
                MessageBox.Show("Vui lòng nhập ID ứng dụng.", "Thiếu thông tin", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Validate ID format
            if (!Regex.IsMatch(IdTextBox.Text, @"^[a-z0-9\-]+$"))
            {
                MessageBox.Show("ID chỉ được chứa chữ thường, số và dấu gạch ngang.", "Lỗi định dạng", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(DownloadUrlTextBox.Text))
            {
                MessageBox.Show("Vui lòng nhập link download.", "Thiếu thông tin", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Validate HTTPS
            if (!DownloadUrlTextBox.Text.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                MessageBox.Show("Link download phải bắt đầu bằng https://", "Lỗi định dạng", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Validate detection (at least one method)
            if (string.IsNullOrWhiteSpace(RegistryKeyTextBox.Text) && string.IsNullOrWhiteSpace(FilePathTextBox.Text))
            {
                MessageBox.Show("Vui lòng nhập ít nhất một phương thức phát hiện (Registry hoặc File).", "Thiếu thông tin", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Check if ID already exists
            var appsDir = _remoteManifestService.GetLocalAppsDirectory();
            var filePath = Path.Combine(appsDir, $"{IdTextBox.Text.Trim()}.yaml");

            if (File.Exists(filePath))
            {
                MessageBox.Show($"App với ID '{IdTextBox.Text.Trim()}' đã tồn tại.\nVui lòng chọn ID khác.", "ID trùng", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                // Create manifest object
                var manifest = new AppManifest
                {
                    Name = NameTextBox.Text.Trim(),
                    Id = IdTextBox.Text.Trim(),
                    Homepage = HomepageTextBox.Text.Trim(),
                    Download = new DownloadConfig
                    {
                        Url = DownloadUrlTextBox.Text.Trim()
                    },
                    Install = new InstallConfig
                    {
                        Type = ((ComboBoxItem)InstallTypeComboBox.SelectedItem).Content.ToString(),
                        SilentArgs = SilentArgsTextBox.Text.Trim()
                    },
                    Detection = new DetectionConfig(),
                    Metadata = new MetadataConfig
                    {
                        Description = DescriptionTextBox.Text.Trim(),
                        Publisher = PublisherTextBox.Text.Trim(),
                        Category = ((ComboBoxItem)CategoryComboBox.SelectedItem).Content.ToString()
                    }
                };

                // Detection
                if (!string.IsNullOrWhiteSpace(RegistryKeyTextBox.Text))
                {
                    manifest.Detection.Registry = new System.Collections.Generic.List<RegistryDetection>
                    {
                        new RegistryDetection { Path = RegistryKeyTextBox.Text.Trim() }
                    };
                }

                if (!string.IsNullOrWhiteSpace(FilePathTextBox.Text))
                {
                    manifest.Detection.File = new System.Collections.Generic.List<FileDetection>
                    {
                        new FileDetection { Path = FilePathTextBox.Text.Trim() }
                    };
                }

                // Save to YAML file
                var serializer = new SerializerBuilder()
                    .WithNamingConvention(UnderscoredNamingConvention.Instance)
                    .Build();

                var yaml = serializer.Serialize(manifest);
                File.WriteAllText(filePath, yaml);

                MessageBox.Show($"Đã thêm app '{manifest.Name}' thành công!\nFile: {filePath}", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi tạo app: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void AutoFillButton_Click(object sender, RoutedEventArgs e)
        {
            var url = HomepageTextBox.Text.Trim();

            if (string.IsNullOrEmpty(url))
            {
                MessageBox.Show("Vui lòng nhập URL trang chủ trước.", "Thiếu URL",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                HomepageTextBox.Focus();
                return;
            }

            if (!url.StartsWith("http://") && !url.StartsWith("https://"))
            {
                MessageBox.Show("URL phải bắt đầu với http:// hoặc https://", "URL không hợp lệ",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            AutoFillButton.IsEnabled = false;
            AutoFillButton.Content = "Đang tải...";

            try
            {
                using var httpClient = new System.Net.Http.HttpClient();
                httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
                httpClient.Timeout = TimeSpan.FromSeconds(10);

                var html = await httpClient.GetStringAsync(url);

                // Extract title from <title> tag
                var titleMatch = System.Text.RegularExpressions.Regex.Match(html, @"<title[^>]*>(.*?)</title>",
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase);

                if (titleMatch.Success && string.IsNullOrEmpty(NameTextBox.Text))
                {
                    var title = System.Net.WebUtility.HtmlDecode(titleMatch.Groups[1].Value.Trim());
                    // Clean up common suffixes
                    title = System.Text.RegularExpressions.Regex.Replace(title, @"\s*[-|]\s*(Home|Official|Download|Free).*$", "",
                        System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                    NameTextBox.Text = title.Trim();
                }

                // Extract description from meta tags
                var descMatch = System.Text.RegularExpressions.Regex.Match(html,
                    @"<meta\s+(?:name|property)=[""'](?:description|og:description)[""']\s+content=[""'](.*?)[""']",
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase);

                if (descMatch.Success && string.IsNullOrEmpty(DescriptionTextBox.Text))
                {
                    var desc = System.Net.WebUtility.HtmlDecode(descMatch.Groups[1].Value.Trim());
                    DescriptionTextBox.Text = desc;
                }

                // Try to find download link
                var downloadMatch = System.Text.RegularExpressions.Regex.Match(html,
                    @"<a[^>]+href=[""'](https?://[^""']*(?:download|install|setup|installer)[^""']*\.(?:exe|msi|zip))[""']",
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase);

                if (downloadMatch.Success && string.IsNullOrEmpty(DownloadUrlTextBox.Text))
                {
                    DownloadUrlTextBox.Text = downloadMatch.Groups[1].Value;
                }

                MessageBox.Show("Đã tự động điền thông tin từ trang web.\n\nVui lòng kiểm tra và điều chỉnh nếu cần.",
                    "Hoàn tất", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Không thể lấy thông tin từ URL:\n{ex.Message}\n\nVui lòng điền thủ công.",
                    "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            finally
            {
                AutoFillButton.IsEnabled = true;
                AutoFillButton.Content = "Tự động điền";
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
