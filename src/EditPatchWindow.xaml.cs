using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using SilentSetup.Models;
using YamlDotNet.Serialization;

namespace SilentSetup
{
    public partial class EditPatchWindow : Window
    {
        private readonly PatchManifest _patch;
        private readonly string _manifestPath;

        public EditPatchWindow(PatchManifest patch, string manifestPath)
        {
            InitializeComponent();
            _patch = patch;
            _manifestPath = manifestPath;
            LoadPatchToForm();
        }

        private void LoadPatchToForm()
        {
            NameTextBox.Text = _patch.Name;
            IdTextBox.Text = _patch.Id;
            TargetAppTextBox.Text = _patch.TargetApp;
            DescriptionTextBox.Text = ""; // PatchManifest doesn't have Description field
            PatchDirectoryText.Text = _patch.PatchDirectory;

            // Set patch type and show appropriate panel
            switch (_patch.Type?.ToLower())
            {
                case "copy-files":
                    PatchTypeComboBox.SelectedIndex = 0;
                    if (_patch.Files?.Any() == true)
                    {
                        DestinationTextBox.Text = _patch.Files[0].Destination ?? "{app_dir}";
                    }
                    break;
                case "executable":
                    PatchTypeComboBox.SelectedIndex = 1;
                    ExeFileTextBox.Text = _patch.Execute?.File ?? "";
                    ExeArgsTextBox.Text = _patch.Execute?.ExecArgs?.FirstOrDefault() ?? "";
                    break;
                case "registry":
                    PatchTypeComboBox.SelectedIndex = 2;
                    if (_patch.Registry?.Any() == true)
                    {
                        var reg = _patch.Registry[0];
                        RegRootTextBox.Text = reg.Root ?? "HKCU";
                        RegPathTextBox.Text = reg.Path ?? "";
                        RegNameTextBox.Text = reg.Name ?? "";
                        RegValueTextBox.Text = reg.Value ?? "";
                        RegTypeTextBox.Text = reg.Type ?? "string";
                    }
                    break;
                case "extract-archive":
                    PatchTypeComboBox.SelectedIndex = 3;
                    if (_patch.Archive != null)
                    {
                        ArchiveFileTextBox.Text = _patch.Archive.File ?? "";
                        ExtractDestTextBox.Text = _patch.Archive.ExtractDir ?? "{app_dir}";
                        ArchivePasswordTextBox.Text = _patch.Archive.Password ?? "";
                    }
                    break;
            }
        }

        private void PatchTypeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (PatchTypeComboBox.SelectedItem == null) return;

            // Hide all panels
            CopyFilesPanel.Visibility = Visibility.Collapsed;
            ExecutablePanel.Visibility = Visibility.Collapsed;
            RegistryPanel.Visibility = Visibility.Collapsed;
            ExtractPanel.Visibility = Visibility.Collapsed;

            // Show selected panel
            var tag = ((ComboBoxItem)PatchTypeComboBox.SelectedItem).Tag.ToString();
            switch (tag)
            {
                case "copy-files":
                    CopyFilesPanel.Visibility = Visibility.Visible;
                    break;
                case "executable":
                    ExecutablePanel.Visibility = Visibility.Visible;
                    break;
                case "registry":
                    RegistryPanel.Visibility = Visibility.Visible;
                    break;
                case "extract-archive":
                    ExtractPanel.Visibility = Visibility.Visible;
                    break;
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            // Validate
            if (string.IsNullOrWhiteSpace(NameTextBox.Text))
            {
                MessageBox.Show("Vui lòng nhập tên patch.", "Thiếu thông tin", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(TargetAppTextBox.Text))
            {
                MessageBox.Show("Vui lòng nhập target app ID.", "Thiếu thông tin", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                // Update manifest
                _patch.Name = NameTextBox.Text.Trim();
                _patch.TargetApp = TargetAppTextBox.Text.Trim();

                var selectedType = ((ComboBoxItem)PatchTypeComboBox.SelectedItem).Tag.ToString();
                _patch.Type = selectedType;

                // Clear all type-specific data
                _patch.Files = null;
                _patch.Execute = null;
                _patch.Registry = null;
                _patch.Archive = null;

                // Set type-specific data
                switch (selectedType)
                {
                    case "copy-files":
                        // Get all files from the files/ directory
                        var filesDir = Path.Combine(_patch.PatchDirectory, "files");
                        if (Directory.Exists(filesDir))
                        {
                            var files = Directory.GetFiles(filesDir)
                                .Select(f => new PatchFile
                                {
                                    Name = Path.GetFileName(f),
                                    Destination = Path.Combine(DestinationTextBox.Text.Trim(), Path.GetFileName(f))
                                })
                                .ToList();
                            _patch.Files = files;
                        }
                        break;

                    case "executable":
                        _patch.Execute = new ExecuteConfig
                        {
                            File = ExeFileTextBox.Text.Trim(),
                            ExecArgs = new System.Collections.Generic.List<string> { ExeArgsTextBox.Text.Trim() }
                        };
                        break;

                    case "registry":
                        _patch.Registry = new System.Collections.Generic.List<RegistryOperation>
                        {
                            new RegistryOperation
                            {
                                Action = "set",
                                Root = RegRootTextBox.Text.Trim(),
                                Path = RegPathTextBox.Text.Trim(),
                                Name = RegNameTextBox.Text.Trim(),
                                Value = RegValueTextBox.Text.Trim(),
                                Type = RegTypeTextBox.Text.Trim()
                            }
                        };
                        break;

                    case "extract-archive":
                        _patch.Archive = new ArchiveConfig
                        {
                            File = ArchiveFileTextBox.Text.Trim(),
                            ExtractDir = ExtractDestTextBox.Text.Trim(),
                            Password = string.IsNullOrEmpty(ArchivePasswordTextBox.Text.Trim()) ? null : ArchivePasswordTextBox.Text.Trim()
                        };
                        break;
                }

                // Serialize to YAML
                var serializer = new SerializerBuilder().Build();
                var yaml = serializer.Serialize(_patch);

                File.WriteAllText(_manifestPath, yaml);

                MessageBox.Show("Đã lưu thay đổi patch.", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi lưu patch: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
