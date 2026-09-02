using System.IO;
using System.Net.Http;
using System.Text.Json;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using SilentSetup.Models;

namespace SilentSetup.Services;

public class RemoteManifestService
{
    private readonly HttpClient _httpClient;
    private readonly LoggerService _logger;
    private readonly string _cacheDir;
    private readonly string _cacheMetaFile;
    private const string GITHUB_RAW_URL = "https://raw.githubusercontent.com/hieuck/Silent-Setup/main/apps";
    private const string GITHUB_API_URL = "https://api.github.com/repos/hieuck/Silent-Setup/contents/apps";
    private const string GITHUB_PATCHES_API_URL = "https://api.github.com/repos/hieuck/Silent-Setup/contents/patches";
    private const int CACHE_HOURS = 24;

    public RemoteManifestService(LoggerService logger)
    {
        _logger = logger;
        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "SilentSetup/1.0");

        // Use exe directory for portable app
        var exeDir = AppContext.BaseDirectory;
        _cacheDir = Path.Combine(exeDir, "cache");
        _cacheMetaFile = Path.Combine(_cacheDir, "cache_meta.json");

        Directory.CreateDirectory(_cacheDir);
    }

    public async Task<bool> UpdateCacheAsync()
    {
        try
        {
            _logger.Info("Fetching manifest list from GitHub API...");

            // Download apps
            var response = await _httpClient.GetStringAsync(GITHUB_API_URL);
            var files = JsonSerializer.Deserialize<List<GitHubFile>>(response);

            if (files == null)
            {
                _logger.Error("Failed to parse GitHub API response");
                return false;
            }

            var manifestFiles = files.Where(f => f.name.EndsWith(".yaml", StringComparison.OrdinalIgnoreCase)).ToList();
            _logger.Info($"Found {manifestFiles.Count} app manifests on GitHub");

            var downloadedCount = 0;
            foreach (var file in manifestFiles)
            {
                try
                {
                    var content = await _httpClient.GetStringAsync(file.download_url);
                    var localPath = Path.Combine(_cacheDir, file.name);
                    await File.WriteAllTextAsync(localPath, content);
                    downloadedCount++;
                    _logger.Info($"Downloaded: {file.name}");
                }
                catch (Exception ex)
                {
                    _logger.Error($"Failed to download {file.name}: {ex.Message}");
                }
            }

            // Download patches
            var patchesDownloaded = await DownloadPatchesAsync();
            _logger.Info($"Downloaded {patchesDownloaded} patch manifests");

            // Get latest commit SHA
            var commitSha = await GetLatestCommitShaAsync();

            // Update cache metadata
            var cacheMeta = new CacheMetadata
            {
                LastUpdate = DateTime.UtcNow,
                FileCount = downloadedCount,
                PatchCount = patchesDownloaded,
                CommitSha = commitSha
            };
            var metaJson = JsonSerializer.Serialize(cacheMeta, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(_cacheMetaFile, metaJson);

            _logger.Info($"Cache updated successfully: {downloadedCount} apps, {patchesDownloaded} patches (SHA: {commitSha?[..7]})");
            return true;
        }
        catch (Exception ex)
        {
            _logger.Error($"Cache update failed: {ex.Message}");
            return false;
        }
    }

    private async Task<int> DownloadPatchesAsync()
    {
        try
        {
            var response = await _httpClient.GetStringAsync(GITHUB_PATCHES_API_URL);
            var folders = JsonSerializer.Deserialize<List<GitHubFile>>(response);

            if (folders == null) return 0;

            var patchFolders = folders.Where(f => f.type == "dir" && !f.name.StartsWith("_")).ToList();
            _logger.Info($"Found {patchFolders.Count} patch folders on GitHub");

            var downloadedCount = 0;
            foreach (var folder in patchFolders)
            {
                try
                {
                    // Get manifest.yaml from patch folder
                    var manifestUrl = $"https://api.github.com/repos/hieuck/Silent-Setup/contents/patches/{folder.name}/manifest.yaml";
                    var manifestResponse = await _httpClient.GetStringAsync(manifestUrl);
                    var manifestFile = JsonSerializer.Deserialize<GitHubFile>(manifestResponse);

                    if (manifestFile != null && !string.IsNullOrEmpty(manifestFile.download_url))
                    {
                        var content = await _httpClient.GetStringAsync(manifestFile.download_url);

                        // Parse YAML to get target_app
                        var deserializer = new DeserializerBuilder()
                            .WithNamingConvention(UnderscoredNamingConvention.Instance)
                            .Build();
                        var patch = deserializer.Deserialize<PatchManifest>(content);

                        if (patch != null && !string.IsNullOrEmpty(patch.TargetApp))
                        {
                            // Save to cache/patches/{target_app}/{patch_id}.yaml
                            var appPatchesDir = Path.Combine(_cacheDir, "patches", patch.TargetApp);
                            Directory.CreateDirectory(appPatchesDir);
                            var localPath = Path.Combine(appPatchesDir, $"{folder.name}.yaml");
                            await File.WriteAllTextAsync(localPath, content);
                            downloadedCount++;
                            _logger.Info($"Downloaded patch: {folder.name} -> {patch.TargetApp}");
                        }
                        else
                        {
                            _logger.Warn($"Patch {folder.name} has no target_app, skipping");
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.Error($"Failed to download patch {folder.name}: {ex.Message}");
                }
            }

            return downloadedCount;
        }
        catch (Exception ex)
        {
            _logger.Error($"Failed to download patches: {ex.Message}");
            return 0;
        }
    }

    public bool IsCacheStale()
    {
        if (!File.Exists(_cacheMetaFile))
            return true;

        try
        {
            var metaJson = File.ReadAllText(_cacheMetaFile);
            var cacheMeta = JsonSerializer.Deserialize<CacheMetadata>(metaJson);

            if (cacheMeta == null) return true;

            var age = DateTime.UtcNow - cacheMeta.LastUpdate;
            return age.TotalHours > CACHE_HOURS;
        }
        catch
        {
            return true;
        }
    }

    public string GetCacheDirectory()
    {
        return _cacheDir;
    }

    public bool HasCache()
    {
        return Directory.Exists(_cacheDir) &&
               Directory.GetFiles(_cacheDir, "*.yaml").Length > 0;
    }

    public async Task<CacheComparisonResult> CompareWithRemoteAsync()
    {
        try
        {
            _logger.Info("Comparing local cache with remote repository...");

            // Get remote file list
            var response = await _httpClient.GetStringAsync(GITHUB_API_URL);
            var remoteFiles = JsonSerializer.Deserialize<List<GitHubFile>>(response);

            if (remoteFiles == null)
            {
                return new CacheComparisonResult { Success = false, Message = "Failed to fetch remote file list" };
            }

            var remoteManifests = remoteFiles.Where(f => f.name.EndsWith(".yaml", StringComparison.OrdinalIgnoreCase))
                                            .Select(f => f.name)
                                            .ToList();

            // Get local cache files
            var localFiles = Directory.Exists(_cacheDir)
                ? Directory.GetFiles(_cacheDir, "*.yaml").Select(Path.GetFileName).Where(f => f != null).Cast<string>().ToList()
                : new List<string>();

            // Compare
            var missingInLocal = remoteManifests.Except(localFiles).ToList();
            var extraInLocal = localFiles.Except(remoteManifests).ToList();
            var common = remoteManifests.Intersect(localFiles).ToList();

            _logger.Info($"Comparison result: Remote={remoteManifests.Count}, Local={localFiles.Count}, Missing={missingInLocal.Count}, Extra={extraInLocal.Count}");

            return new CacheComparisonResult
            {
                Success = true,
                RemoteCount = remoteManifests.Count,
                LocalCount = localFiles.Count,
                MissingInLocal = missingInLocal,
                ExtraInLocal = extraInLocal,
                InSync = missingInLocal.Count == 0 && extraInLocal.Count == 0,
                Message = missingInLocal.Count == 0 && extraInLocal.Count == 0
                    ? "Cache is in sync with remote"
                    : $"Cache out of sync: {missingInLocal.Count} missing, {extraInLocal.Count} extra"
            };
        }
        catch (Exception ex)
        {
            _logger.Error($"Cache comparison failed: {ex.Message}");
            return new CacheComparisonResult { Success = false, Message = ex.Message };
        }
    }

    public string GetLocalAppsDirectory()
    {
        var exeDir = AppContext.BaseDirectory;
        var localAppsDir = Path.Combine(exeDir, "local_apps");
        Directory.CreateDirectory(localAppsDir);
        return localAppsDir;
    }

    public string GetLocalPatchesDirectory()
    {
        var exeDir = AppContext.BaseDirectory;
        var localPatchesDir = Path.Combine(exeDir, "local_patches");
        Directory.CreateDirectory(localPatchesDir);
        return localPatchesDir;
    }

    public async Task<string?> GetLatestCommitShaAsync()
    {
        try
        {
            var response = await _httpClient.GetStringAsync("https://api.github.com/repos/hieuck/Silent-Setup/commits/main");
            var commit = JsonSerializer.Deserialize<JsonElement>(response);
            return commit.GetProperty("sha").GetString();
        }
        catch (Exception ex)
        {
            _logger.Error($"Failed to fetch latest commit SHA: {ex.Message}");
            return null;
        }
    }

    public async Task<bool> HasRemoteUpdatesAsync()
    {
        var remoteSha = await GetLatestCommitShaAsync();
        if (remoteSha == null) return false;

        if (!File.Exists(_cacheMetaFile)) return true;

        try
        {
            var metaJson = File.ReadAllText(_cacheMetaFile);
            var cacheMeta = JsonSerializer.Deserialize<CacheMetadata>(metaJson);

            return cacheMeta?.CommitSha != remoteSha;
        }
        catch
        {
            return true;
        }
    }

    public class CacheComparisonResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = "";
        public int RemoteCount { get; set; }
        public int LocalCount { get; set; }
        public List<string> MissingInLocal { get; set; } = new();
        public List<string> ExtraInLocal { get; set; } = new();
        public bool InSync { get; set; }
    }

    private class GitHubFile
    {
        public string name { get; set; } = "";
        public string download_url { get; set; } = "";
        public string type { get; set; } = "";
    }

    private class CacheMetadata
    {
        public DateTime LastUpdate { get; set; }
        public int FileCount { get; set; }
        public int PatchCount { get; set; }
        public string? CommitSha { get; set; }
    }
}
