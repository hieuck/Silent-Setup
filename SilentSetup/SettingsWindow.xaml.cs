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
            PatchListBox.Items.Clear();
            foreach (var patch in _patches)
            {
                PatchListBox.Items.Add($"{patch.Name} ({patch.TargetApp})");
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
                    try
                    {
                        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                        {
                            FileName = manifestPath,
                            UseShellExecute = true
                        });
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Không thể mở file: {ex.Message}", "Lỗi",
                            MessageBoxButton.OK, MessageBoxImage.Error);
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
            MessageBox.Show("Cài đặt đã được lưu.", "Thành công",
                MessageBoxButton.OK, MessageBoxImage.Information);
            DialogResult = true;
            Close();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
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
