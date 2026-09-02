using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using SilentSetup.Models;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace SilentSetup
{
    public partial class EditAppWindow : Window
    {
        private readonly AppManifest _manifest;
        private readonly string _filePath;

        public EditAppWindow(AppManifest manifest, string filePath)
        {
            InitializeComponent();
            _manifest = manifest;
            _filePath = filePath;
            LoadManifestToForm();
        }

        private void LoadManifestToForm()
        {
            // Basic info
            NameTextBox.Text = _manifest.Name ?? "";
            IdTextBox.Text = _manifest.Id ?? "";
            HomepageTextBox.Text = _manifest.Homepage ?? "";

            // Download
            DownloadUrlTextBox.Text = _manifest.Download?.Url ?? "";

            // Install
            if (_manifest.Install != null)
            {
                var installType = _manifest.Install.Type?.ToLower() ?? "exe";
                InstallTypeComboBox.SelectedIndex = installType switch
                {
                    "msi" => 1,
                    "zip" => 2,
                    _ => 0
                };
                SilentArgsTextBox.Text = _manifest.Install.SilentArgs ?? "";
            }

            // Detection
            if (_manifest.Detection?.Registry != null && _manifest.Detection.Registry.Count > 0)
            {
                RegistryKeyTextBox.Text = _manifest.Detection.Registry[0].Path ?? "";
            }
            if (_manifest.Detection?.File != null && _manifest.Detection.File.Count > 0)
            {
                FilePathTextBox.Text = _manifest.Detection.File[0].Path ?? "";
            }

            // Metadata
            if (_manifest.Metadata != null)
            {
                DescriptionTextBox.Text = _manifest.Metadata.Description ?? "";
                PublisherTextBox.Text = _manifest.Metadata.Publisher ?? "";

                var category = _manifest.Metadata.Category ?? "Utility";
                CategoryComboBox.SelectedIndex = category switch
                {
                    "Browser" => 0,
                    "Development" => 1,
                    "Media" => 2,
                    _ => 3
                };
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            // Validate required fields
            if (string.IsNullOrWhiteSpace(NameTextBox.Text))
            {
                MessageBox.Show("Vui lòng nhập tên ứng dụng.", "Thiếu thông tin", MessageBoxButton.OK, MessageBoxImage.Warning);
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

            try
            {
                // Update manifest object
                _manifest.Name = NameTextBox.Text.Trim();
                _manifest.Homepage = HomepageTextBox.Text.Trim();

                // Download
                if (_manifest.Download == null)
                    _manifest.Download = new DownloadConfig();
                _manifest.Download.Url = DownloadUrlTextBox.Text.Trim();

                // Install
                if (_manifest.Install == null)
                    _manifest.Install = new InstallConfig();

                _manifest.Install.Type = ((ComboBoxItem)InstallTypeComboBox.SelectedItem).Content.ToString();
                _manifest.Install.SilentArgs = SilentArgsTextBox.Text.Trim();

                // Detection
                if (_manifest.Detection == null)
                    _manifest.Detection = new DetectionConfig();

                if (!string.IsNullOrWhiteSpace(RegistryKeyTextBox.Text))
                {
                    _manifest.Detection.Registry = new System.Collections.Generic.List<RegistryDetection>
                    {
                        new RegistryDetection { Path = RegistryKeyTextBox.Text.Trim() }
                    };
                }
                else
                {
                    _manifest.Detection.Registry = null;
                }

                if (!string.IsNullOrWhiteSpace(FilePathTextBox.Text))
                {
                    _manifest.Detection.File = new System.Collections.Generic.List<FileDetection>
                    {
                        new FileDetection { Path = FilePathTextBox.Text.Trim() }
                    };
                }
                else
                {
                    _manifest.Detection.File = null;
                }

                // Metadata
                if (_manifest.Metadata == null)
                    _manifest.Metadata = new MetadataConfig();

                _manifest.Metadata.Description = DescriptionTextBox.Text.Trim();
                _manifest.Metadata.Publisher = PublisherTextBox.Text.Trim();
                _manifest.Metadata.Category = ((ComboBoxItem)CategoryComboBox.SelectedItem).Content.ToString();

                // Save to YAML file
                SaveManifestToFile();

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi lưu: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SaveManifestToFile()
        {
            var serializer = new SerializerBuilder()
                .WithNamingConvention(UnderscoredNamingConvention.Instance)
                .Build();

            var yaml = serializer.Serialize(_manifest);
            File.WriteAllText(_filePath, yaml);
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
