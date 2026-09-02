using Microsoft.Win32;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text.RegularExpressions;
using SilentSetup.Models;

namespace SilentSetup.Services
{
    /// <summary>
    /// Detects installed applications and their versions
    /// </summary>
    public class DetectionService
    {
        private readonly LoggerService _logger;

        public DetectionService(LoggerService logger)
        {
            _logger = logger;
        }

        public AppStatus DetectApp(AppManifest app)
        {
            try
            {
                if (app.Detection == null)
                {
                    _logger.Warn($"No detection config for {app.Name}");
                    return AppStatus.Unknown;
                }

                bool isInstalled = false;
                string? version = null;

                switch (app.Detection.Method.ToLower())
                {
                    case "registry":
                        (isInstalled, version) = DetectViaRegistry(app);
                        break;

                    case "file":
                        (isInstalled, version) = DetectViaFile(app);
                        break;

                    case "both":
                        var (regInstalled, regVersion) = DetectViaRegistry(app);
                        var (fileInstalled, fileVersion) = DetectViaFile(app);
                        isInstalled = regInstalled || fileInstalled;
                        version = regVersion ?? fileVersion;
                        break;

                    default:
                        _logger.Warn($"Unknown detection method for {app.Name}: {app.Detection.Method}");
                        return AppStatus.Unknown;
                }

                if (isInstalled)
                {
                    app.InstalledVersion = version;

                    // Check for updates if version URL is provided
                    if (!string.IsNullOrWhiteSpace(app.Download.VersionUrl))
                    {
                        var latestVersion = CheckLatestVersion(app);
                        app.LatestVersion = latestVersion;

                        if (!string.IsNullOrWhiteSpace(latestVersion) &&
                            !string.IsNullOrWhiteSpace(version) &&
                            latestVersion != version)
                        {
                            _logger.Info($"{app.Name} update available: {version} → {latestVersion}");
                            return AppStatus.UpdateAvailable;
                        }
                    }

                    _logger.Info($"{app.Name} detected: installed (version: {version ?? "unknown"})");
                    return AppStatus.Installed;
                }

                _logger.Info($"{app.Name} detected: not installed");
                return AppStatus.NotInstalled;
            }
            catch (Exception ex)
            {
                _logger.Error($"Detection failed for {app.Name}: {ex.Message}");
                return AppStatus.Unknown;
            }
        }

        private (bool installed, string? version) DetectViaRegistry(AppManifest app)
        {
            if (app.Detection?.Registry == null || !app.Detection.Registry.Any())
                return (false, null);

            foreach (var reg in app.Detection.Registry)
            {
                try
                {
                    var expandedPath = Environment.ExpandEnvironmentVariables(reg.Path);

                    // Try to open registry key
                    using var key = OpenRegistryKey(expandedPath);
                    if (key == null)
                        continue;

                    // Key exists - app is installed
                    if (string.IsNullOrWhiteSpace(reg.Value))
                        return (true, null);

                    // Try to read version
                    var versionValue = key.GetValue(reg.Value);
                    if (versionValue != null)
                    {
                        return (true, versionValue.ToString());
                    }

                    return (true, null);
                }
                catch (Exception ex)
                {
                    _logger.Warn($"Registry detection failed for {reg.Path}: {ex.Message}");
                }
            }

            return (false, null);
        }

        private (bool installed, string? version) DetectViaFile(AppManifest app)
        {
            if (app.Detection?.File == null || !app.Detection.File.Any())
                return (false, null);

            foreach (var file in app.Detection.File)
            {
                try
                {
                    var expandedPath = Environment.ExpandEnvironmentVariables(file.Path);

                    if (!System.IO.File.Exists(expandedPath))
                        continue;

                    // File exists - app is installed
                    string? version = null;

                    if (file.VersionSource == "file_properties")
                    {
                        version = GetFileVersion(expandedPath);
                    }
                    else if (file.VersionSource == "file_content")
                    {
                        version = ExtractVersionFromFile(expandedPath, file.VersionRegex);
                    }

                    return (true, version);
                }
                catch (Exception ex)
                {
                    _logger.Warn($"File detection failed for {file.Path}: {ex.Message}");
                }
            }

            return (false, null);
        }

        private RegistryKey? OpenRegistryKey(string path)
        {
            // Parse path like "HKLM\\SOFTWARE\\..."
            var parts = path.Split(new[] { '\\' }, 2);
            if (parts.Length < 2)
                return null;

            RegistryKey? root = parts[0].ToUpper() switch
            {
                "HKLM" => Registry.LocalMachine,
                "HKCU" => Registry.CurrentUser,
                "HKCR" => Registry.ClassesRoot,
                "HKU" => Registry.Users,
                "HKCC" => Registry.CurrentConfig,
                _ => null
            };

            if (root == null)
                return null;

            try
            {
                return root.OpenSubKey(parts[1], writable: false);
            }
            catch
            {
                return null;
            }
        }

        private string? GetFileVersion(string filePath)
        {
            try
            {
                var versionInfo = FileVersionInfo.GetVersionInfo(filePath);
                return versionInfo.FileVersion ?? versionInfo.ProductVersion;
            }
            catch
            {
                return null;
            }
        }

        private string? ExtractVersionFromFile(string filePath, string? regex)
        {
            if (string.IsNullOrWhiteSpace(regex))
                return null;

            try
            {
                var content = System.IO.File.ReadAllText(filePath);
                var match = Regex.Match(content, regex);

                if (match.Success && match.Groups.Count > 1)
                {
                    return match.Groups[1].Value;
                }
            }
            catch
            {
                // Ignore
            }

            return null;
        }

        private string? CheckLatestVersion(AppManifest app)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(app.Download.VersionUrl))
                    return null;

                _logger.Info($"Checking latest version for {app.Name} from {app.Download.VersionUrl}");

                using var httpClient = new HttpClient();
                httpClient.Timeout = TimeSpan.FromSeconds(10);

                var response = httpClient.GetStringAsync(app.Download.VersionUrl).Result;

                // If version regex is provided, extract version from response
                if (!string.IsNullOrWhiteSpace(app.Download.VersionRegex))
                {
                    var regex = new Regex(app.Download.VersionRegex);
                    var match = regex.Match(response);
                    if (match.Success && match.Groups.Count > 1)
                    {
                        var version = match.Groups[1].Value.Trim();
                        _logger.Info($"Latest version extracted: {version}");
                        return version;
                    }
                }
                else
                {
                    // Assume the response is just the version string
                    var version = response.Trim();
                    if (!string.IsNullOrWhiteSpace(version) && version.Length < 50)
                    {
                        _logger.Info($"Latest version: {version}");
                        return version;
                    }
                }

                _logger.Warn($"Could not extract version from response");
                return null;
            }
            catch (Exception ex)
            {
                _logger.Warn($"Failed to check latest version for {app.Name}: {ex.Message}");
                return null;
            }
        }

        public void RefreshAllApps(List<AppManifest> apps)
        {
            _logger.Info($"Refreshing detection status for {apps.Count} apps");

            foreach (var app in apps)
            {
                app.Status = DetectApp(app);
            }

            _logger.Info("Detection refresh complete");
        }
    }
}
