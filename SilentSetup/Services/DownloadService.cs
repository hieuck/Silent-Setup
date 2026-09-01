using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using SilentSetup.Models;

namespace SilentSetup.Services
{
    /// <summary>
    /// Downloads installers with caching and checksum verification
    /// </summary>
    public class DownloadService
    {
        private readonly LoggerService _logger;
        private readonly HttpClient _httpClient;
        private readonly string _cacheDirectory;

        public DownloadService(LoggerService logger, AppConfig config)
        {
            _logger = logger;
            _cacheDirectory = config.Download.CacheDir;
            Directory.CreateDirectory(_cacheDirectory);

            _httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(config.Download.TimeoutSeconds)
            };
        }

        public async Task<DownloadResult> DownloadAsync(AppManifest app, IProgress<int>? progress = null)
        {
            try
            {
                _logger.Info($"Starting download: {app.Name}");

                // Generate cache filename
                var fileName = GenerateCacheFileName(app);
                var cachedPath = Path.Combine(_cacheDirectory, fileName);

                // Check cache
                if (File.Exists(cachedPath))
                {
                    _logger.Info($"Found in cache: {cachedPath}");

                    // Verify checksum if available
                    if (!string.IsNullOrWhiteSpace(app.Download.Checksum))
                    {
                        if (VerifyChecksum(cachedPath, app.Download.Checksum))
                        {
                            _logger.Info("Checksum verified, using cached file");
                            return new DownloadResult
                            {
                                Success = true,
                                Message = "Using cached installer",
                                FilePath = cachedPath,
                                FileSize = new FileInfo(cachedPath).Length,
                                ChecksumVerified = true
                            };
                        }
                        else
                        {
                            _logger.Warn("Checksum mismatch, re-downloading");
                            File.Delete(cachedPath);
                        }
                    }
                    else
                    {
                        // No checksum, assume cache is valid
                        return new DownloadResult
                        {
                            Success = true,
                            Message = "Using cached installer",
                            FilePath = cachedPath,
                            FileSize = new FileInfo(cachedPath).Length,
                            ChecksumVerified = false
                        };
                    }
                }

                // Download from URL
                var downloadUrl = app.Download.Url;
                var downloaded = await DownloadFile(downloadUrl, cachedPath, progress);

                if (!downloaded)
                {
                    // Try mirrors if available
                    if (app.Download.Mirrors != null && app.Download.Mirrors.Any())
                    {
                        _logger.Warn("Primary URL failed, trying mirrors");
                        foreach (var mirror in app.Download.Mirrors)
                        {
                            _logger.Info($"Trying mirror: {mirror}");
                            downloaded = await DownloadFile(mirror, cachedPath, progress);
                            if (downloaded)
                            {
                                downloadUrl = mirror;
                                break;
                            }
                        }
                    }

                    if (!downloaded)
                    {
                        return new DownloadResult
                        {
                            Success = false,
                            Message = "Download failed from all URLs",
                            ErrorDetails = "All download attempts failed"
                        };
                    }
                }

                // Verify checksum
                bool checksumOk = true;
                if (!string.IsNullOrWhiteSpace(app.Download.Checksum))
                {
                    checksumOk = VerifyChecksum(cachedPath, app.Download.Checksum);
                    if (!checksumOk)
                    {
                        _logger.Error("Checksum verification failed");
                        File.Delete(cachedPath);
                        return new DownloadResult
                        {
                            Success = false,
                            Message = "Checksum verification failed",
                            ErrorDetails = "Downloaded file checksum does not match expected value"
                        };
                    }
                }

                _logger.Info($"Download complete: {cachedPath}");

                return new DownloadResult
                {
                    Success = true,
                    Message = "Download successful",
                    FilePath = cachedPath,
                    FileSize = new FileInfo(cachedPath).Length,
                    ChecksumVerified = checksumOk
                };
            }
            catch (Exception ex)
            {
                _logger.Error($"Download failed for {app.Name}: {ex.Message}");
                return new DownloadResult
                {
                    Success = false,
                    Message = "Download error",
                    ErrorDetails = ex.Message
                };
            }
        }

        private async Task<bool> DownloadFile(string url, string outputPath, IProgress<int>? progress)
        {
            try
            {
                using var response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
                response.EnsureSuccessStatusCode();

                var totalBytes = response.Content.Headers.ContentLength ?? 0;
                var buffer = new byte[8192];
                long downloadedBytes = 0;

                using var contentStream = await response.Content.ReadAsStreamAsync();
                using var fileStream = new FileStream(outputPath, FileMode.Create, FileAccess.Write, FileShare.None);

                while (true)
                {
                    var bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length);
                    if (bytesRead == 0)
                        break;

                    await fileStream.WriteAsync(buffer, 0, bytesRead);
                    downloadedBytes += bytesRead;

                    // Report progress
                    if (progress != null && totalBytes > 0)
                    {
                        var percentage = (int)((downloadedBytes * 100) / totalBytes);
                        progress.Report(percentage);
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.Warn($"Download failed from {url}: {ex.Message}");
                if (File.Exists(outputPath))
                {
                    File.Delete(outputPath);
                }
                return false;
            }
        }

        private bool VerifyChecksum(string filePath, string expectedChecksum)
        {
            try
            {
                using var sha256 = SHA256.Create();
                using var stream = File.OpenRead(filePath);
                var hash = sha256.ComputeHash(stream);
                var actualChecksum = BitConverter.ToString(hash).Replace("-", "").ToLower();

                var expected = expectedChecksum.ToLower().Replace(" ", "").Replace("-", "");
                return actualChecksum == expected;
            }
            catch (Exception ex)
            {
                _logger.Error($"Checksum verification error: {ex.Message}");
                return false;
            }
        }

        private string GenerateCacheFileName(AppManifest app)
        {
            // Extract filename from URL or generate one
            var uri = new Uri(app.Download.Url);
            var urlFileName = Path.GetFileName(uri.LocalPath);

            if (!string.IsNullOrWhiteSpace(urlFileName) && urlFileName.Length > 3)
            {
                // Use URL filename with app ID prefix
                var extension = Path.GetExtension(urlFileName);
                return $"{app.Id}_{urlFileName}";
            }

            // Generate filename from app ID and type
            var ext = app.Install.Type switch
            {
                "msi" => ".msi",
                "zip" => ".zip",
                _ => ".exe"
            };

            return $"{app.Id}_installer{ext}";
        }

        public void ClearCache()
        {
            try
            {
                var files = Directory.GetFiles(_cacheDirectory);
                foreach (var file in files)
                {
                    File.Delete(file);
                }
                _logger.Info($"Cache cleared: {files.Length} files deleted");
            }
            catch (Exception ex)
            {
                _logger.Error($"Failed to clear cache: {ex.Message}");
            }
        }

        public long GetCacheSize()
        {
            try
            {
                var files = Directory.GetFiles(_cacheDirectory);
                return files.Sum(f => new FileInfo(f).Length);
            }
            catch
            {
                return 0;
            }
        }
    }
}
