namespace SilentSetup.Models
{
    /// <summary>
    /// Represents an application manifest loaded from apps/*.yaml
    /// </summary>
    public class AppManifest
    {
        // Required fields
        public string Name { get; set; } = string.Empty;
        public string Id { get; set; } = string.Empty;
        public string Homepage { get; set; } = string.Empty;
        public DownloadConfig Download { get; set; } = new();
        public InstallConfig Install { get; set; } = new();

        // Optional fields
        public DetectionConfig? Detection { get; set; }
        public MetadataConfig? Metadata { get; set; }
        public AdvancedConfig? Advanced { get; set; }

        // Runtime properties (not from YAML)
        public AppStatus Status { get; set; } = AppStatus.Unknown;
        public string? InstalledVersion { get; set; }
        public string? LatestVersion { get; set; }
    }

    public class DownloadConfig
    {
        public string Url { get; set; } = string.Empty;
        public string? VersionUrl { get; set; }
        public string? VersionRegex { get; set; }
        public int? SizeMb { get; set; }
        public string? Checksum { get; set; }
        public List<string>? Mirrors { get; set; }
    }

    public class InstallConfig
    {
        public string Type { get; set; } = "exe"; // exe, msi, zip
        public string SilentArgs { get; set; } = string.Empty;
        public string? InstallDir { get; set; }
        public int Timeout { get; set; } = 600;
        public bool RequireAdmin { get; set; } = true;
    }

    public class DetectionConfig
    {
        public string Method { get; set; } = "registry"; // registry, file, both
        public List<RegistryDetection>? Registry { get; set; }
        public List<FileDetection>? File { get; set; }
    }

    public class RegistryDetection
    {
        public string Path { get; set; } = string.Empty;
        public string? Value { get; set; }
    }

    public class FileDetection
    {
        public string Path { get; set; } = string.Empty;
        public string VersionSource { get; set; } = "file_properties"; // file_properties, file_content
        public string? VersionRegex { get; set; }
    }

    public class MetadataConfig
    {
        public string? Category { get; set; }
        public string? Publisher { get; set; }
        public string? License { get; set; }
        public string? Description { get; set; }
        public List<string>? Tags { get; set; }
        public string? LastUpdated { get; set; }
        public string? Maintainer { get; set; }
    }

    public class AdvancedConfig
    {
        public PreInstallConfig? PreInstall { get; set; }
        public PostInstallConfig? PostInstall { get; set; }
        public UninstallConfig? Uninstall { get; set; }
        public List<DependencyConfig>? Dependencies { get; set; }
    }

    public class PreInstallConfig
    {
        public List<string>? KillProcesses { get; set; }
        public List<string>? Cleanup { get; set; }
    }

    public class PostInstallConfig
    {
        public List<ShortcutConfig>? Shortcuts { get; set; }
        public bool SetDefaultBrowser { get; set; }
    }

    public class ShortcutConfig
    {
        public string Name { get; set; } = string.Empty;
        public string Target { get; set; } = string.Empty;
        public string Location { get; set; } = "desktop";
    }

    public class UninstallConfig
    {
        public string Command { get; set; } = string.Empty;
        public string? Args { get; set; }
    }

    public class DependencyConfig
    {
        public string Id { get; set; } = string.Empty;
        public bool Optional { get; set; }
    }

    public enum AppStatus
    {
        Unknown,
        NotInstalled,
        Installed,
        UpdateAvailable
    }
}
