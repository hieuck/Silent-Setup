namespace SilentSetup.Models
{
    /// <summary>
    /// Application configuration loaded from config.json
    /// </summary>
    public class AppConfig
    {
        public DownloadSettings Download { get; set; } = new();
        public InstallSettings Install { get; set; } = new();
        public ProxySettings Proxy { get; set; } = new();
        public UiSettings Ui { get; set; } = new();
        public Dictionary<string, List<string>> Profiles { get; set; } = new();
    }

    public class DownloadSettings
    {
        public string CacheDir { get; set; } = "cache";
        public int CacheSizeLimitMb { get; set; } = 5000;
        public int TimeoutSeconds { get; set; } = 300;
        public int RetryCount { get; set; } = 3;
    }

    public class InstallSettings
    {
        public int TimeoutSeconds { get; set; } = 600;
        public bool RunAsAdmin { get; set; } = true;
        public bool VerifyAfterInstall { get; set; } = true;
    }

    public class ProxySettings
    {
        public bool Enabled { get; set; }
        public string Url { get; set; } = string.Empty;
    }

    public class UiSettings
    {
        public string Language { get; set; } = "en";
        public string Theme { get; set; } = "light";
    }
}
