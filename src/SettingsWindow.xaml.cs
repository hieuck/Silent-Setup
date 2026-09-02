using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using SilentSetup.Models;

namespace SilentSetup
{
    public partial class SettingsWindow : Window
    {
        private readonly List<PatchManifest> _patches;
        private readonly List<string> _customCategories = new();

        public SettingsWindow(List<PatchManifest> patches)
        {
            InitializeComponent();
            _patches = patches;
            LoadSettings();
        }

        private void LoadSettings()
        {
            // Load patches
            PatchListBox.Items.Clear();
            foreach (var patch in _patches)
            {
                PatchListBox.Items.Add($"{patch.Name} ({patch.TargetApp})");
            }

            // Load categories
            CategoryListBox.Items.Clear();

            // Add default categories
            var defaultCategories = new[] { "Browser", "Development", "Media", "Utility" };
            foreach (var cat in defaultCategories)
            {
                var item = new ListBoxItem { Content = cat, Tag = "default" };
                CategoryListBox.Items.Add(item);
            }

            // Load custom categories from config
            var configPath = Path.Combine(Directory.GetCurrentDirectory(), "config.json");
            if (File.Exists(configPath))
            {
                try
                {
                    var json = File.ReadAllText(configPath);
                    var config = System.Text.Json.JsonDocument.Parse(json);

                    if (config.RootElement.TryGetProperty("ui", out var ui) &&
                        ui.TryGetProperty("custom_categories", out var categories))
                    {
                        foreach (var cat in categories.EnumerateArray())
                        {
                            var categoryName = cat.GetString();
                            if (!string.IsNullOrEmpty(categoryName))
                            {
                                var item = new ListBoxItem { Content = categoryName, Tag = "custom" };
                                CategoryListBox.Items.Add(item);
                                _customCategories.Add(categoryName);
                            }
                        }
                    }
                }
                catch
                {
                    // Ignore errors loading config
                }
            }
        }

        private void AddCategoryButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new InputDialog("Thêm danh mục", "Nhập tên danh mục mới:");
            if (dialog.ShowDialog() == true)
            {
                var category = dialog.ResponseText.Trim();
                if (!string.IsNullOrEmpty(category))
                {
                    var exists = CategoryListBox.Items.Cast<ListBoxItem>()
                        .Any(item => item.Content.ToString() == category);

                    if (!exists)
                    {
                        var item = new ListBoxItem { Content = category, Tag = "custom" };
                        CategoryListBox.Items.Add(item);
                        _customCategories.Add(category);
                    }
                    else
                    {
                        MessageBox.Show("Danh mục đã tồn tại.", "Thông báo",
                            MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
            }
        }

        private void RemoveCategoryButton_Click(object sender, RoutedEventArgs e)
        {
            if (CategoryListBox.SelectedItem is ListBoxItem item)
            {
                if (item.Tag?.ToString() == "default")
                {
                    MessageBox.Show("Không thể xóa danh mục mặc định.", "Lỗi",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var result = MessageBox.Show(
                    $"Xóa danh mục '{item.Content}'?",
                    "Xác nhận",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    CategoryListBox.Items.Remove(item);
                    _customCategories.Remove(item.Content.ToString()!);
                }
            }
            else
            {
                MessageBox.Show("Vui lòng chọn danh mục cần xóa.", "Thông báo",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void EditPatchButton_Click(object sender, RoutedEventArgs e)
        {
            if (PatchListBox.SelectedIndex >= 0)
            {
                var patch = _patches[PatchListBox.SelectedIndex];
                var manifestPath = Path.Combine(patch.PatchDirectory, "manifest.yaml");

                if (File.Exists(manifestPath))
                {
                    var editWindow = new EditPatchWindow(patch, manifestPath);
                    if (editWindow.ShowDialog() == true)
                    {
                        // Refresh patch list
                        LoadSettings();
                    }
                }
            }
            else
            {
                MessageBox.Show("Vui lòng chọn patch cần chỉnh sửa.", "Thông báo",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void DeletePatchButton_Click(object sender, RoutedEventArgs e)
        {
            if (PatchListBox.SelectedIndex >= 0)
            {
                var patch = _patches[PatchListBox.SelectedIndex];
                var result = MessageBox.Show(
                    $"Xóa patch '{patch.Name}'?\n\nThư mục {patch.PatchDirectory} sẽ bị xóa vĩnh viễn.",
                    "Xác nhận xóa",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        if (Directory.Exists(patch.PatchDirectory))
                        {
                            Directory.Delete(patch.PatchDirectory, true);
                            MessageBox.Show($"Đã xóa patch '{patch.Name}'", "Thành công",
                                MessageBoxButton.OK, MessageBoxImage.Information);

                            _patches.RemoveAt(PatchListBox.SelectedIndex);
                            LoadSettings();
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Lỗi khi xóa: {ex.Message}", "Lỗi",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            else
            {
                MessageBox.Show("Vui lòng chọn patch cần xóa.", "Thông báo",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void OpenPatchFolderButton_Click(object sender, RoutedEventArgs e)
        {
            if (PatchListBox.SelectedIndex >= 0)
            {
                var patch = _patches[PatchListBox.SelectedIndex];
                if (Directory.Exists(patch.PatchDirectory))
                {
                    try
                    {
                        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                        {
                            FileName = patch.PatchDirectory,
                            UseShellExecute = true
                        });
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Không thể mở thư mục: {ex.Message}", "Lỗi",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            else
            {
                MessageBox.Show("Vui lòng chọn patch.", "Thông báo",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Save custom categories to config.json
                var configPath = Path.Combine(Directory.GetCurrentDirectory(), "config.json");

                System.Text.Json.JsonDocument? config = null;
                if (File.Exists(configPath))
                {
                    var json = File.ReadAllText(configPath);
                    config = System.Text.Json.JsonDocument.Parse(json);
                }

                // Build new config with updated categories
                using var stream = new MemoryStream();
                using var writer = new System.Text.Json.Utf8JsonWriter(stream, new System.Text.Json.JsonWriterOptions { Indented = true });

                writer.WriteStartObject();

                // Copy existing properties
                if (config != null && config.RootElement.ValueKind == System.Text.Json.JsonValueKind.Object)
                {
                    foreach (var property in config.RootElement.EnumerateObject())
                    {
                        if (property.Name == "ui")
                        {
                            // Write ui section with updated custom_categories
                            writer.WritePropertyName("ui");
                            writer.WriteStartObject();

                            foreach (var uiProp in property.Value.EnumerateObject())
                            {
                                if (uiProp.Name == "custom_categories")
                                {
                                    writer.WritePropertyName("custom_categories");
                                    writer.WriteStartArray();
                                    foreach (var cat in _customCategories)
                                    {
                                        writer.WriteStringValue(cat);
                                    }
                                    writer.WriteEndArray();
                                }
                                else
                                {
                                    writer.WritePropertyName(uiProp.Name);
                                    uiProp.Value.WriteTo(writer);
                                }
                            }

                            writer.WriteEndObject();
                        }
                        else
                        {
                            writer.WritePropertyName(property.Name);
                            property.Value.WriteTo(writer);
                        }
                    }
                }

                writer.WriteEndObject();
                writer.Flush();

                var updatedJson = System.Text.Encoding.UTF8.GetString(stream.ToArray());
                File.WriteAllText(configPath, updatedJson);

                MessageBox.Show("Cài đặt đã được lưu.", "Thành công",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi lưu cài đặt: {ex.Message}", "Lỗi",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void Hyperlink_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
        {
            try
            {
                var uri = e.Uri.ToString();
                if (uri.StartsWith("http://") || uri.StartsWith("https://"))
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = uri,
                        UseShellExecute = true
                    });
                }
                else
                {
                    var fullPath = Path.Combine(Directory.GetCurrentDirectory(), uri);
                    if (File.Exists(fullPath))
                    {
                        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                        {
                            FileName = fullPath,
                            UseShellExecute = true
                        });
                    }
                }
                e.Handled = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Không thể mở: {ex.Message}", "Lỗi",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void CheckUpdateButton_Click(object sender, RoutedEventArgs e)
        {
            CheckUpdateButton.IsEnabled = false;
            UpdateStatusText.Text = "Đang kiểm tra...";
            UpdateStatusText.Foreground = System.Windows.Media.Brushes.Gray;

            try
            {
                using var httpClient = new System.Net.Http.HttpClient();
                httpClient.DefaultRequestHeaders.Add("User-Agent", "SilentSetup");
                httpClient.Timeout = TimeSpan.FromSeconds(10);

                // Check GitHub releases API
                var response = await httpClient.GetStringAsync("https://api.github.com/repos/yourusername/silent-setup/releases/latest");

                // Parse version from response (simple check for "tag_name")
                if (response.Contains("\"tag_name\""))
                {
                    var startIndex = response.IndexOf("\"tag_name\":\"") + 12;
                    var endIndex = response.IndexOf("\"", startIndex);
                    var latestVersion = response.Substring(startIndex, endIndex - startIndex).TrimStart('v');

                    var currentVersion = "1.0.0";
                    if (latestVersion != currentVersion)
                    {
                        UpdateStatusText.Text = $"Có bản cập nhật mới: {latestVersion}";
                        UpdateStatusText.Foreground = System.Windows.Media.Brushes.Green;

                        var result = MessageBox.Show(
                            $"Có phiên bản mới {latestVersion} (hiện tại: {currentVersion})\n\nMở trang download?",
                            "Cập nhật có sẵn",
                            MessageBoxButton.YesNo,
                            MessageBoxImage.Information);

                        if (result == MessageBoxResult.Yes)
                        {
                            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                            {
                                FileName = "https://github.com/yourusername/silent-setup/releases/latest",
                                UseShellExecute = true
                            });
                        }
                    }
                    else
                    {
                        UpdateStatusText.Text = "Bạn đang dùng phiên bản mới nhất";
                        UpdateStatusText.Foreground = System.Windows.Media.Brushes.Green;
                    }
                }
            }
            catch (Exception ex)
            {
                UpdateStatusText.Text = $"Lỗi khi kiểm tra: {ex.Message}";
                UpdateStatusText.Foreground = System.Windows.Media.Brushes.Red;
            }
            finally
            {
                CheckUpdateButton.IsEnabled = true;
            }
        }

        private void SubmitFeedbackButton_Click(object sender, RoutedEventArgs e)
        {
            var type = ((ComboBoxItem)FeedbackTypeComboBox.SelectedItem)?.Content?.ToString() ?? "Khác";
            var title = FeedbackTitleTextBox.Text.Trim();
            var content = FeedbackContentTextBox.Text.Trim();
            var email = FeedbackEmailTextBox.Text.Trim();

            if (string.IsNullOrEmpty(title))
            {
                MessageBox.Show("Vui lòng nhập tiêu đề.", "Thiếu thông tin",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                FeedbackTitleTextBox.Focus();
                return;
            }

            if (string.IsNullOrEmpty(content))
            {
                MessageBox.Show("Vui lòng nhập nội dung chi tiết.", "Thiếu thông tin",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                FeedbackContentTextBox.Focus();
                return;
            }

            // Build GitHub issue URL
            var issueTitle = $"[{type}] {title}";
            var issueBody = $"**Loại góp ý:** {type}\n\n**Nội dung:**\n{content}\n\n";
            if (!string.IsNullOrEmpty(email))
            {
                issueBody += $"**Email liên hệ:** {email}\n\n";
            }
            issueBody += "---\n_Gửi từ Silent Setup v1.0.0_";

            var encodedTitle = Uri.EscapeDataString(issueTitle);
            var encodedBody = Uri.EscapeDataString(issueBody);
            var issueUrl = $"https://github.com/yourusername/silent-setup/issues/new?title={encodedTitle}&body={encodedBody}";

            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = issueUrl,
                    UseShellExecute = true
                });

                MessageBox.Show(
                    "Trình duyệt sẽ mở trang GitHub Issues.\n\nVui lòng đăng nhập GitHub và nhấn 'Submit new issue'.",
                    "Mở trình duyệt",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                // Clear form
                FeedbackTitleTextBox.Clear();
                FeedbackContentTextBox.Clear();
                FeedbackEmailTextBox.Clear();
                FeedbackTypeComboBox.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Không thể mở trình duyệt: {ex.Message}", "Lỗi",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    public class InputDialog : Window
    {
        private TextBox _textBox;
        public string ResponseText => _textBox.Text;

        public InputDialog(string title, string prompt)
        {
            Title = title;
            Width = 400;
            Height = 150;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            ResizeMode = ResizeMode.NoResize;

            var grid = new Grid { Margin = new Thickness(15) };
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var label = new TextBlock { Text = prompt, Margin = new Thickness(0, 0, 0, 10) };
            Grid.SetRow(label, 0);
            grid.Children.Add(label);

            _textBox = new TextBox { Height = 30, VerticalContentAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 0, 15) };
            Grid.SetRow(_textBox, 1);
            grid.Children.Add(_textBox);

            var buttonPanel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
            var okButton = new Button { Content = "OK", Width = 80, Height = 30, Margin = new Thickness(0, 0, 10, 0), IsDefault = true };
            okButton.Click += (s, e) => { DialogResult = true; Close(); };
            var cancelButton = new Button { Content = "Hủy", Width = 80, Height = 30, IsCancel = true };
            cancelButton.Click += (s, e) => { DialogResult = false; Close(); };
            buttonPanel.Children.Add(okButton);
            buttonPanel.Children.Add(cancelButton);
            Grid.SetRow(buttonPanel, 2);
            grid.Children.Add(buttonPanel);

            Content = grid;
        }
    }
}
