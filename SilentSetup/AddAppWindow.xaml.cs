using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace SilentSetup;

public partial class AddAppWindow : Window
{
    public AddAppWindow()
    {
        InitializeComponent();
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        // Validate required fields
        if (string.IsNullOrWhiteSpace(NameTextBox.Text))
        {
            MessageBox.Show("Vui lòng nhập tên ứng dụng.", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (string.IsNullOrWhiteSpace(IdTextBox.Text))
        {
            MessageBox.Show("Vui lòng nhập ID.", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (string.IsNullOrWhiteSpace(DownloadUrlTextBox.Text))
        {
            MessageBox.Show("Vui lòng nhập link download.", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (!DownloadUrlTextBox.Text.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            MessageBox.Show("Link download phải dùng HTTPS.", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        // Validate ID format
        var id = IdTextBox.Text.ToLower().Trim();
        if (!System.Text.RegularExpressions.Regex.IsMatch(id, @"^[a-z0-9-]+$"))
        {
            MessageBox.Show("ID chỉ được chứa chữ thường, số và dấu gạch ngang.", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        // Generate YAML
        var yaml = GenerateYaml();

        // Save to apps/ directory
        try
        {
            var appsDir = Path.Combine(Directory.GetCurrentDirectory(), "apps");
            Directory.CreateDirectory(appsDir);

            var fileName = $"{id}.yaml";
            var filePath = Path.Combine(appsDir, fileName);

            if (File.Exists(filePath))
            {
                var result = MessageBox.Show($"File {fileName} đã tồn tại. Ghi đè?", "Xác nhận",
                    MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result != MessageBoxResult.Yes)
                    return;
            }

            File.WriteAllText(filePath, yaml, Encoding.UTF8);

            MessageBox.Show($"Đã lưu: {fileName}\n\nClick 'Làm mới' để load ứng dụng mới.", "Thành công",
                MessageBoxButton.OK, MessageBoxImage.Information);

            DialogResult = true;
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Lỗi khi lưu file:\n{ex.Message}", "Lỗi",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private string GenerateYaml()
    {
        var sb = new StringBuilder();

        // Basic info
        sb.AppendLine($"name: {NameTextBox.Text.Trim()}");
        sb.AppendLine($"id: {IdTextBox.Text.ToLower().Trim()}");
        sb.AppendLine($"homepage: {HomepageTextBox.Text.Trim()}");
        sb.AppendLine();

        // Download
        sb.AppendLine("download:");
        sb.AppendLine($"  url: {DownloadUrlTextBox.Text.Trim()}");
        sb.AppendLine("  checksum: \"\"");
        sb.AppendLine("  mirrors: []");
        sb.AppendLine();

        // Install
        sb.AppendLine("install:");
        var installType = ((ComboBoxItem)InstallTypeComboBox.SelectedItem)?.Content?.ToString()?.ToLower() ?? "exe";
        sb.AppendLine($"  type: {installType}");
        sb.AppendLine($"  silent_args: {SilentArgsTextBox.Text.Trim()}");
        sb.AppendLine("  pre_install:");
        sb.AppendLine("    kill_processes: []");
        sb.AppendLine("  post_install:");
        sb.AppendLine("    create_shortcuts: []");
        sb.AppendLine();

        // Detection
        sb.AppendLine("detection:");
        var hasRegistry = !string.IsNullOrWhiteSpace(RegistryKeyTextBox.Text);
        var hasFile = !string.IsNullOrWhiteSpace(FilePathTextBox.Text);

        if (hasRegistry && hasFile)
            sb.AppendLine("  method: both");
        else if (hasRegistry)
            sb.AppendLine("  method: registry");
        else if (hasFile)
            sb.AppendLine("  method: file");
        else
            sb.AppendLine("  method: registry");

        if (hasRegistry)
        {
            sb.AppendLine("  registry:");
            // Escape backslashes for YAML
            var regKey = RegistryKeyTextBox.Text.Trim().Replace("\\", "\\\\");
            sb.AppendLine($"    - path: {regKey}");
            sb.AppendLine("      value: \"\"");
        }

        if (hasFile)
        {
            sb.AppendLine("  file:");
            sb.AppendLine($"    - path: {FilePathTextBox.Text.Trim()}");
        }

        sb.AppendLine();

        // Metadata
        sb.AppendLine("metadata:");
        if (!string.IsNullOrWhiteSpace(DescriptionTextBox.Text))
            sb.AppendLine($"  description: {DescriptionTextBox.Text.Trim()}");
        if (!string.IsNullOrWhiteSpace(PublisherTextBox.Text))
            sb.AppendLine($"  publisher: {PublisherTextBox.Text.Trim()}");
        var category = ((ComboBoxItem)CategoryComboBox.SelectedItem)?.Content?.ToString() ?? "Utility";
        sb.AppendLine($"  category: {category}");
        sb.AppendLine("  license: Freeware");
        sb.AppendLine("  tags: []");

        return sb.ToString();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
